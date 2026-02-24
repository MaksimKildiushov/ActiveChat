using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>Сообщение в диалоге (входящее или исходящее).</summary>
[Table("Messages")]
public class MessageEntity : IntEntity
{
    /// <summary>Направление: входящее или исходящее.</summary>
    [MaxLength(16)]
    public MessageDirection Direction { get; set; }

    /// <summary>Текст сообщения.</summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>Исходный JSON сообщения с канала (аудит).</summary>
    [Column(TypeName = "jsonb")]
    public string? RawJson { get; set; }

    /// <summary>Время отправки по данным канала.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Диалог, к которому относится сообщение.</summary>
    public int ConversationId { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;
}
