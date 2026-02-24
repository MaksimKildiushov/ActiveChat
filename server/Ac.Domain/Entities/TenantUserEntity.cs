using Ac.Domain.Entities.Abstract;
using Ac.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>
/// Связь пользователь ↔ тенант с ролью внутри тенанта.
/// Composite PK: (TenantId, UserId) — один пользователь имеет одну роль в каждом тенанте.
/// </summary>
[Table("TenantUsers")]
public class TenantUserEntity : BaseEntity
{
    /// <summary>Тенант.</summary>
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;

    /// <summary>Пользователь.</summary>
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(UserEntity.TenantMemberships))]
    public UserEntity User { get; set; } = null!;

    /// <summary>Роль пользователя в тенанте.</summary>
    public TenantRole Role { get; set; }
}
