using Ac.Domain.Entities;
using Ac.Domain.Enums;
using Ac.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ac.Data;

/// <summary>
/// Контекст данных одного тенанта. Все тенант-специфичные таблицы (Conversations, Messages, DecisionAudits)
/// маппятся в схему <see cref="ICurrentTenantContext.SchemaName"/>. Tenants и Channels из public — только для чтения (навигация).
/// </summary>
public class TenantDb : DbContext
{
    /// <summary>Схема для design-time (ef migrations add) и для раннера миграций.</summary>
    public const string DesignTimeSchema = "tenant_template";

    private readonly ICurrentTenantContext? _tenantContext;
    private readonly string? _schemaNameForDesignTime;

    /// <summary>Рантайм: схема берётся из контекста запроса.</summary>
    public TenantDb(DbContextOptions<TenantDb> options, ICurrentTenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
        _schemaNameForDesignTime = null;
    }

    /// <summary>Design-time и раннер миграций: схема задаётся явно (например <see cref="DesignTimeSchema"/>).</summary>
    public TenantDb(DbContextOptions<TenantDb> options, string schemaNameForDesignTime)
        : base(options)
    {
        _tenantContext = null;
        _schemaNameForDesignTime = schemaNameForDesignTime ?? throw new ArgumentNullException(nameof(schemaNameForDesignTime));
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<DecisionAuditEntity> DecisionAudits => Set<DecisionAuditEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ChannelType>().HaveConversion<string>();
        configurationBuilder.Properties<StepKind>().HaveConversion<string>();
        configurationBuilder.Properties<MessageDirection>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = _schemaNameForDesignTime ?? _tenantContext?.SchemaName;
        if (string.IsNullOrWhiteSpace(schema))
            throw new InvalidOperationException(
                "Tenant schema is not set. Set ICurrentTenantContext (e.g. in InboundPipeline) before using TenantDb.");

        #region Public: Tenants, Channels — только для чтения, не участвуют в миграциях TenantDb

        modelBuilder.Ignore<TenantUserEntity>();

        modelBuilder.Entity<TenantEntity>(b =>
        {
            b.ToTable("Tenants", "public");
            b.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        modelBuilder.Entity<ChannelEntity>(b =>
        {
            b.ToTable("Channels", "public");
            b.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        #endregion

        #region Auth: UserEntity — для FK AuthorId/ModifierId из тенант-таблиц

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("AspNetUsers", "auth");
            b.Metadata.SetIsTableExcludedFromMigrations(true);
        });

        #endregion

        #region Tenant schema: Conversations, Messages, DecisionAudits

        modelBuilder.Entity<ConversationEntity>(b =>
        {
            b.ToTable("Conversations", schema);
            b.HasOne(c => c.Channel)
                .WithMany()
                .HasForeignKey(c => c.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MessageEntity>(b =>
        {
            b.ToTable("Messages", schema);
        });

        modelBuilder.Entity<DecisionAuditEntity>(b =>
        {
            b.ToTable("DecisionAudits", schema);
        });

        #endregion

        #region UTC DateTime converters (как в ApiDb)

        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind != DateTimeKind.Utc ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        var utcDateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(),
            v => v.ToUniversalTime());

        var nullableUtcDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? v.Value.ToUniversalTime() : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless) continue;

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
