using Ac.Domain.Entities;
using Ac.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ac.Data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<DecisionAuditEntity> DecisionAudits => Set<DecisionAuditEntity>();

    /// <summary>
    /// Глобальные конвенции типов — применяются ко всем сущностям автоматически.
    /// Размер колонки берётся из [MaxLength] на свойстве в entity-классе.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ChannelType>().HaveConversion<string>();
        configurationBuilder.Properties<StepKind>().HaveConversion<string>();
        configurationBuilder.Properties<MessageDirection>().HaveConversion<string>();
    }

    /// <summary>
    /// Остаётся только то, что нельзя выразить атрибутами:
    ///   — UTC-конвертеры для DateTime / DateTimeOffset
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region UTC DateTime converters

        // Все даты хранятся и читаются как UTC.
        // DateTimeOffset добавлен отдельно — Npgsql сам мапит его на timestamptz (UTC),
        // но конвертер гарантирует UTC-offset на уровне приложения независимо от провайдера.

        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind != DateTimeKind.Utc ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? v.Value.Kind != DateTimeKind.Utc ? v.Value.ToUniversalTime() : v
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v);

        var utcDateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(),
            v => v.ToUniversalTime());

        var nullableUtcDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? v.Value.ToUniversalTime() : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless)
                continue;

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcDateTimeConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcDateTimeConverter);
                else if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(utcDateTimeOffsetConverter);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(nullableUtcDateTimeOffsetConverter);
            }
        }

        #endregion
    }
}
