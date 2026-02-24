using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ac.Data;

/// <summary>
/// Используется EF Core при выполнении <c>dotnet ef migrations add --context TenantDb</c>.
/// Схема тенанта задаётся как <see cref="TenantDb.DesignTimeSchema"/> (миграции генерируются для одной шаблонной схемы).
/// Применение по всем схемам тенантов — через страницу админки «Миграции тенантов» или при запуске Admin с аргументом migrate_tenants.
/// </summary>
public sealed class TenantDbDesignTimeFactory : IDesignTimeDbContextFactory<TenantDb>
{
    public TenantDb CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found. Run from the API project directory or set ConnectionStrings__Default.");

        var optionsBuilder = new DbContextOptionsBuilder<TenantDb>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TenantDb(optionsBuilder.Options, TenantDb.DesignTimeSchema);
    }
}
