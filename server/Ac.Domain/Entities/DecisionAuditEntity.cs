using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ac.Domain.Enums;

namespace Ac.Domain.Entities;

[Table("DecisionAudits")]
public class DecisionAuditEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    // Конверсия enum → string настраивается глобально в ApiDbContext.ConfigureConventions
    [MaxLength(64)]
    public StepKind StepKind { get; set; }

    public double Confidence { get; set; }

    [Column(TypeName = "jsonb")]
    public string? SlotsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;
}
