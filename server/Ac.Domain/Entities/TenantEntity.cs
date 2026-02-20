using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("Tenants")]
public class TenantEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ChannelEntity> Channels { get; set; } = [];
}
