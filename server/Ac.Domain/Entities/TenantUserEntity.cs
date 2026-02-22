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
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(UserEntity.TenantMemberships))]
    public UserEntity User { get; set; } = null!;

    public TenantRole Role { get; set; }
}
