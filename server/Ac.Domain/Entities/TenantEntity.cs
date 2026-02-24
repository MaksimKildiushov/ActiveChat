using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>Тенант (организация). Данные тенанта — в отдельной схеме БД (SchemaName). Таблица в public.</summary>
[Table("Tenants")]
public class TenantEntity : IntEntity
{
    /// <summary>ИНН организации (уникальный идентификатор тенанта).</summary>
    [Required]
    [MaxLength(256)]
    public string Inn { get; set; } = string.Empty;

    /// <summary>Имя схемы БД для данных тенанта (без миграций — вычисляется в рантайме).</summary>
    [NotMapped]
    public string SchemaName => "t_" + Inn;

    /// <summary>Каналы тенанта.</summary>
    public ICollection<ChannelEntity> Channels { get; set; } = [];

    /// <summary>Пользователи и их роли в тенанте.</summary>
    public ICollection<TenantUserEntity> Users { get; set; } = [];
}
