using Libraries.Abstractions.Interfaces;
using Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities.Abstract;

/// <summary>Базовая сущность с аудитом (Author, Modifier, Created, Modified).</summary>
public abstract class BaseEntity : IBaseEntity
{
    private List<string>? _props;

    /// <summary>Кто создал запись.</summary>
    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public UserEntity Author { get; set; } = null!;

    /// <summary>Дата создания.</summary>
    public DateTime Created { get; set; }

    /// <summary>Дата последнего изменения.</summary>
    public DateTime? Modified { get; set; }

    /// <summary>Кто последний изменил запись.</summary>
    public Guid? ModifierId { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public UserEntity? Modifier { get; set; }

    /// <summary>Список имён свойств и значений (для отладки/логирования).</summary>
    [NotMapped]
    public List<string> AllProperties
    {
        get => _props == null
            ? _props = PropertiesHlp.AllAndValues(this)
            : _props;
    }
}
