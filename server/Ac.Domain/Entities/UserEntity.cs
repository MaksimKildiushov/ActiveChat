using Libraries.Abstractions.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    public string? DisplayName { get; set; }

    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public UserEntity Author { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public Guid? ModifierId { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public UserEntity? Modifier { get; set; }
}
