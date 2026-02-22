using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

[Table("Channels")]
[Index(nameof(Token), IsUnique = true)]
public class ChannelEntity : KeyEntity<Guid>
{
    // Конверсия Enum → Guid настраивается глобально в ApiDbContext.ConfigureConventions
    [MaxLength(64)]
    public ChannelType ChannelType { get; set; }

    public Guid Token { get; set; }

    [Column(TypeName = "jsonb")]
    public string? SettingsJson { get; set; }

    public bool IsActive { get; set; }

    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;

    public ICollection<ConversationEntity> Conversations { get; set; } = [];
}
