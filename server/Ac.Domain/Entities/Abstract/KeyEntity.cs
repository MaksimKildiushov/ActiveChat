using System.ComponentModel.DataAnnotations;
namespace Ac.Domain.Entities.Abstract;

public abstract class KeyEntity<T> : BaseEntity where T : notnull
{
    [Key]
    public virtual T Id { get; set; } = default!;
}
