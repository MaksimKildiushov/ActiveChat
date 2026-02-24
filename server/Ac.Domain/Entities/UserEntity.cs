using Libraries.Abstractions.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>Пользователь системы (оператор, админ). AspNetUsers в схеме auth.</summary>
public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    /// <summary>Отображаемое имя пользователя.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Членство в тенантах (роли). Пусто у системных и admin-аккаунтов.</summary>
    [InverseProperty(nameof(TenantUserEntity.User))]
    public ICollection<TenantUserEntity> TenantMemberships { get; set; } = [];

    /// <summary>Кто создал запись (для аудита).</summary>
    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public UserEntity Author { get; set; } = null!;

    /// <summary>Дата создания записи.</summary>
    public DateTime Created { get; set; }

    /// <summary>Дата последнего изменения.</summary>
    public DateTime? Modified { get; set; }

    /// <summary>Кто последний изменил запись.</summary>
    public Guid? ModifierId { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public UserEntity? Modifier { get; set; }
}
