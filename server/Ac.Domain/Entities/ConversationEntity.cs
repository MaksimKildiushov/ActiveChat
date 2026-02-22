using Ac.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("Conversations")]
[Index(nameof(TenantId), nameof(ChannelId), nameof(ExternalUserId), IsUnique = true)]
public class ConversationEntity : IntEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(256)]
    public string ExternalUserId { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? StateJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int ChannelId { get; set; }

    [ForeignKey(nameof(ChannelId))]
    public ChannelEntity Channel { get; set; } = null!;

    public ICollection<MessageEntity> Messages { get; set; } = [];

    public ICollection<DecisionAuditEntity> DecisionAudits { get; set; } = [];
}
