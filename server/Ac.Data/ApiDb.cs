using Ac.Domain.Entities;
using Ac.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ac.Data;

public class ApiDb(DbContextOptions<ApiDb> options)
    : IdentityDbContext<UserEntity, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<TenantUserEntity> TenantUsers => Set<TenantUserEntity>();
    public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    public DbSet<EventEntity> Events => Set<EventEntity>();

    /// <summary>
    /// Глобальные конвенции типов — применяются ко всем сущностям автоматически.
    /// Размер колонки берётся из [MaxLength] на свойстве в entity-классе.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ChannelType>().HaveConversion<string>();
        configurationBuilder.Properties<StepKind>().HaveConversion<string>();
        configurationBuilder.Properties<MessageDirection>().HaveConversion<string>();
        configurationBuilder.Properties<TenantRole>().HaveConversion<string>();
    }

    /// <summary>
    /// Остаётся только то, что нельзя выразить атрибутами:
    ///   — UTC-конвертеры для DateTime / DateTimeOffset
    ///   — схема auth для таблиц Identity
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region TenantUsers composite PK

        modelBuilder.Entity<TenantUserEntity>()
            .HasKey(tu => new { tu.TenantId, tu.UserId });

        // TenantUserEntity наследует BaseEntity, у которой Author/Modifier → UserEntity,
        // плюс собственная навигация User → UserEntity.
        // Три FK к одной таблице — EF Core не разрешает неоднозначность без явной конфигурации.
        modelBuilder.Entity<TenantUserEntity>()
            .HasOne(e => e.Author)
            .WithMany()
            .HasForeignKey(e => e.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TenantUserEntity>()
            .HasOne(e => e.Modifier)
            .WithMany()
            .HasForeignKey(e => e.ModifierId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Public: каталог

        modelBuilder.Entity<TenantEntity>().ToTable("Tenants", "public");
        modelBuilder.Entity<TenantUserEntity>().ToTable("TenantUsers", "public");
        modelBuilder.Entity<ChannelEntity>().ToTable("Channels", "public");
        modelBuilder.Entity<EventEntity>().ToTable("Events", "public");

        #endregion

        #region Auth schema

        modelBuilder.Entity<UserEntity>()             .ToTable("AspNetUsers",      "auth");
        modelBuilder.Entity<IdentityRole<Guid>>()     .ToTable("AspNetRoles",      "auth");
        modelBuilder.Entity<IdentityUserRole<Guid>>() .ToTable("AspNetUserRoles",  "auth");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AspNetUserClaims", "auth");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AspNetUserLogins", "auth");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AspNetUserTokens", "auth");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AspNetRoleClaims", "auth");

        #endregion

        #region AspNetUsers column order
        // Type.GetProperties() не гарантирует порядок для наследуемых типов,
        // поэтому порядок колонок задаётся явно через HasColumnOrder().

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.Property(u => u.Id)                  .HasColumnOrder(0);
            b.Property(u => u.UserName)             .HasColumnOrder(1);
            b.Property(u => u.NormalizedUserName)   .HasColumnOrder(2);
            b.Property(u => u.Email)                .HasColumnOrder(3);
            b.Property(u => u.NormalizedEmail)      .HasColumnOrder(4);
            b.Property(u => u.EmailConfirmed)       .HasColumnOrder(5);
            b.Property(u => u.DisplayName)          .HasColumnOrder(6);
            b.Property(u => u.PasswordHash)         .HasColumnOrder(7);
            b.Property(u => u.SecurityStamp)        .HasColumnOrder(8);
            b.Property(u => u.ConcurrencyStamp)     .HasColumnOrder(9);
            b.Property(u => u.PhoneNumber)          .HasColumnOrder(10);
            b.Property(u => u.PhoneNumberConfirmed) .HasColumnOrder(11);
            b.Property(u => u.TwoFactorEnabled)     .HasColumnOrder(12);
            b.Property(u => u.LockoutEnd)           .HasColumnOrder(13);
            b.Property(u => u.LockoutEnabled)       .HasColumnOrder(14);
            b.Property(u => u.AccessFailedCount)    .HasColumnOrder(15);
            b.Property(u => u.AuthorId)             .HasColumnOrder(16);
            b.Property(u => u.Created)              .HasColumnOrder(17);
            b.Property(u => u.Modified)             .HasColumnOrder(18);
            b.Property(u => u.ModifierId)           .HasColumnOrder(19);
        });

        #endregion

        #region UTC DateTime converters

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
