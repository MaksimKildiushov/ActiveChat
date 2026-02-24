using System.ComponentModel.DataAnnotations;
namespace Ac.Domain.Entities.Abstract;

/// <summary>Базовая сущность с ключом типа T (например int или Guid).</summary>
public abstract class KeyEntity<T> : BaseEntity where T : notnull
{
    /// <summary>Первичный ключ.</summary>
    [Key]
    public virtual T Id { get; set; } = default!;
}
