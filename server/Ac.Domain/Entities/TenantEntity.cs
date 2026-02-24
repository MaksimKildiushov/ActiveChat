using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("Tenants")]
public class TenantEntity : IntEntity
{
    [Required]
    [MaxLength(256)]
    public string Inn { get; set; } = string.Empty;

    /// <summary>Имя схемы БД для данных тенанта (без миграций — вычисляется в рантайме).</summary>
    [NotMapped]
    public string SchemaName => "t_" + Inn;

    public ICollection<ChannelEntity> Channels { get; set; } = [];

    public ICollection<TenantUserEntity> Users { get; set; } = [];
}
