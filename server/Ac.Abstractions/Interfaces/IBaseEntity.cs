namespace Libraries.Abstractions.Interfaces;

/// <summary>
/// Интерфейс базовой сущности с полями аудита
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// ID автора (создателя) записи
    /// </summary>
    Guid AuthorId { get; set; }

    /// <summary>
    /// Дата создания записи
    /// </summary>
    DateTime Created { get; set; }

    /// <summary>
    /// Дата последнего изменения записи
    /// </summary>
    DateTime? Modified { get; set; }

    /// <summary>
    /// ID пользователя, который последним изменил запись
    /// </summary>
    Guid? ModifierId { get; set; }
}
