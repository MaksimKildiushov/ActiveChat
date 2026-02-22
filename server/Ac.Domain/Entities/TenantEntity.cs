using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("Tenants")]
public class TenantEntity : IntEntity
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ChannelEntity> Channels { get; set; } = [];
}
