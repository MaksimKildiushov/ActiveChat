using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Ac.Data;

/// <summary>
/// Применяет миграции TenantDb ко всем схемам тенантов из public.Tenants.
/// Используется страницей админки и при запуске Admin с аргументом migrate_tenants.
/// </summary>
public static class TenantMigrationRunner
{
    /// <summary>Результат применения миграций к одной схеме.</summary>
    public sealed record Result(string Schema, bool Success, string Message, int AppliedCount = 0);

    /// <summary>Проверяет, существует ли схема в БД.</summary>
    public static async Task<bool> SchemaExistsAsync(string connectionString, string schemaName, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """SELECT 1 FROM information_schema.schemata WHERE schema_name = @p""",
            conn);
        cmd.Parameters.AddWithValue("p", schemaName);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null;
    }

    /// <summary>Возвращает имена существующих в БД схем тенантов (схемы вида t_*).</summary>
    public static async Task<IReadOnlySet<string>> GetExistingTenantSchemaNamesAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 't_%'""",
            conn);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            set.Add(reader.GetString(0));

        return set;
    }

    /// <summary>
    /// Возвращает имена схем тенантов, в которых применены миграции (есть таблица __EFMigrationsHistory).
    /// Только такие схемы считаются «готовыми» для работы тенанта.
    /// </summary>
    public static async Task<IReadOnlySet<string>> GetFullyMigratedTenantSchemaNamesAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """SELECT table_schema FROM information_schema.tables WHERE table_schema LIKE 't_%' AND table_name ILIKE '__EFMigrationsHistory'""",
            conn);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            set.Add(reader.GetString(0));

        return set;
    }

    /// <summary>Создаёт схему и применяет миграции TenantDb для одной схемы тенанта.</summary>
    public static async Task<Result> RunForSchemaAsync(string connectionString, string schemaName, CancellationToken ct = default)
    {
        try
        {
            await EnsureSchemaExistsAsync(connectionString, schemaName, ct);

            static void ConfigureTenantOptions(DbContextOptionsBuilder<TenantDb> b, string connStr, string historySchema)
            {
                b.UseNpgsql(connStr, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", historySchema));
                b.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            // 1) Считаем pending-миграции по целевой схеме (t_xxx); история — в схеме тенанта
            var optionsTenant = new DbContextOptionsBuilder<TenantDb>();
            ConfigureTenantOptions(optionsTenant, connectionString, schemaName);
            await using var tenantContext = new TenantDb(optionsTenant.Options, schemaName);
            var pending = await tenantContext.Database.GetPendingMigrationsAsync(ct);
            var count = pending.Count();

            if (count == 0)
            {
                return new Result(schemaName, true, "Схема создана, миграции уже актуальны", 0);
            }

            // 2) Генерируем idempotent-скрипт: история миграций в tenant_template, затем подмена на t_xxx
            var optionsTemplate = new DbContextOptionsBuilder<TenantDb>();
            ConfigureTenantOptions(optionsTemplate, connectionString, TenantDb.DesignTimeSchema);
            await using var templateContext = new TenantDb(optionsTemplate.Options, TenantDb.DesignTimeSchema);
            var migrator = templateContext.Database.GetService<IMigrator>();
            var baseScript = migrator.GenerateScript(
                fromMigration: null,
                toMigration: null,
                options: MigrationsSqlGenerationOptions.Idempotent);

            // 3) Подменяем tenant_template на целевую схему t_xxx
            var scriptForTenant = baseScript.Replace(TenantDb.DesignTimeSchema, schemaName, StringComparison.Ordinal);

            // 4) Выполняем скрипт напрямую через Npgsql
            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync(ct);
                await using var cmd = new NpgsqlCommand(scriptForTenant, conn);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            return new Result(schemaName, true, $"Применено миграций: {count}", count);
        }
        catch (Exception ex)
        {
            return new Result(schemaName, false, ex.Message, 0);
        }
    }

    /// <summary>Возвращает список имён схем тенантов (t_&lt;Inn&gt;) из public.Tenants.</summary>
    public static async Task<IReadOnlyList<string>> GetTenantSchemaNamesAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """SELECT "Inn" FROM public."Tenants" WHERE "Inn" IS NOT NULL AND "Inn" <> ''""",
            conn);

        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var inn = reader.GetString(0);
            list.Add("t_" + inn);
        }

        return list;
    }

    /// <summary>Применяет ожидающие миграции TenantDb ко всем схемам тенантов.</summary>
    public static async Task<IReadOnlyList<Result>> RunAsync(string connectionString, CancellationToken ct = default)
    {
        var schemas = await GetTenantSchemaNamesAsync(connectionString, ct);
        var results = new List<Result>(schemas.Count);

        // Опции для генерации скрипта: история миграций в tenant_template
        var optionsTemplate = new DbContextOptionsBuilder<TenantDb>();
        optionsTemplate.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TenantDb.DesignTimeSchema));
        optionsTemplate.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        await using var templateContext = new TenantDb(optionsTemplate.Options, TenantDb.DesignTimeSchema);
        var migrator = templateContext.Database.GetService<IMigrator>();
        var baseScript = migrator.GenerateScript(
            fromMigration: null,
            toMigration: null,
            options: MigrationsSqlGenerationOptions.Idempotent);

        foreach (var schema in schemas)
        {
            try
            {
                await EnsureSchemaExistsAsync(connectionString, schema, ct);

                // Опции тенанта: история в схеме тенанта (для GetPendingMigrationsAsync)
                var optionsTenant = new DbContextOptionsBuilder<TenantDb>();
                optionsTenant.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
                optionsTenant.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                await using var tenantContext = new TenantDb(optionsTenant.Options, schema);
                var pending = await tenantContext.Database.GetPendingMigrationsAsync(ct);
                var count = pending.Count();

                if (count > 0)
                {
                    var scriptForTenant = baseScript.Replace(TenantDb.DesignTimeSchema, schema, StringComparison.Ordinal);

                    await using (var conn = new NpgsqlConnection(connectionString))
                    {
                        await conn.OpenAsync(ct);
                        await using var cmd = new NpgsqlCommand(scriptForTenant, conn);
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    results.Add(new Result(schema, true, $"Применено миграций: {count}", count));
                }
                else
                {
                    results.Add(new Result(schema, true, "Уже актуально", 0));
                }
            }
            catch (Exception ex)
            {
                results.Add(new Result(schema, false, ex.Message, 0));
            }
        }

        return results;
    }

    private static async Task EnsureSchemaExistsAsync(string connectionString, string schemaName, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"""CREATE SCHEMA IF NOT EXISTS "{schemaName.Replace("\"", "\"\"")}" """,
            conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
