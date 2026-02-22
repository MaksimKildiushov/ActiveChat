using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

[Table("Messages")]
public class MessageEntity : IntEntity
{
    // Конверсия enum → string настраивается глобально в ApiDbContext.ConfigureConventions
    [MaxLength(16)]
    public MessageDirection Direction { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? RawJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int ConversationId { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;
}
