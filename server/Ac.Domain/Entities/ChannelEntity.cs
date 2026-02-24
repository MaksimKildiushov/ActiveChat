using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

[Table("Channels")]
[Index(nameof(Token), IsUnique = true)]
public class ChannelEntity : IntEntity
{
    // Конверсия Enum → string настраивается глобально в ApiDb.ConfigureConventions
    [MaxLength(64)]
    public ChannelType ChannelType { get; set; }

    public Guid Token { get; set; }

    [Column(TypeName = "jsonb")]
    public string? SettingsJson { get; set; }

    public bool IsActive { get; set; }

    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;
}
