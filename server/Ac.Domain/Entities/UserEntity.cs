using Libraries.Abstractions.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Ac.Domain.Entities;

public class UserEntity : IdentityUser<Guid>, IBaseEntity
{
    public string? DisplayName { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Modified { get; set; }
    public Guid? ModifierId { get; set; }
}
