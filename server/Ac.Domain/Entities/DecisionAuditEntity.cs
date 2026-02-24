using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("DecisionAudits")]
public class DecisionAuditEntity : IntEntity
{
    public int ConversationId { get; set; }

    // Конверсия enum → string настраивается глобально в ApiDbContext.ConfigureConventions
    [MaxLength(64)]
    public StepKind StepKind { get; set; }

    public double Confidence { get; set; }

    [Column(TypeName = "jsonb")]
    public string? SlotsJson { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;
}
