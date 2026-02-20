using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ac.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

[Table("Channels")]
[Index(nameof(Token), IsUnique = true)]
public class ChannelEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    // Конверсия enum → string настраивается глобально в ApiDbContext.ConfigureConventions
    [MaxLength(64)]
    public ChannelType ChannelType { get; set; }

    [Required]
    [MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? SettingsJson { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;

    public ICollection<ConversationEntity> Conversations { get; set; } = [];
}
