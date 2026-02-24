using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>Аудит решения ИИ по одному шагу диалога (шаг, уверенность, слоты).</summary>
[Table("DecisionAudits")]
public class DecisionAuditEntity : IntEntity
{
    /// <summary>Диалог, в котором принято решение.</summary>
    public int ConversationId { get; set; }

    /// <summary>Тип шага (ответ, уточнение, ручная передача и т.д.).</summary>
    [MaxLength(64)]
    public StepKind StepKind { get; set; }

    /// <summary>Уверенность модели (0..1).</summary>
    public double Confidence { get; set; }

    /// <summary>Извлечённые слоты в JSON.</summary>
    [Column(TypeName = "jsonb")]
    public string? SlotsJson { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;
}
