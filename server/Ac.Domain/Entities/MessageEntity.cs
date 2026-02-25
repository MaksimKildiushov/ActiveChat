using Ac.Domain.Entities.Abstract;
using Ac.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>Сообщение в диалоге (входящее или исходящее).</summary>
[Table("Messages")]
public class MessageEntity : IntEntity
{
    [Required]
    public string Content { get; set; } = null!;

    /// <summary>
    /// Тип сообщения (текст, изображение, файл, системное).
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// Статус сообщения (отправлено, доставлено, прочитано).
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    /// <summary>Направление: входящее или исходящее.</summary>
    [MaxLength(16)]
    public MessageDirection Direction { get; set; }

    /// <summary>
    /// Путь к файлу (если сообщение содержит файл).
    /// </summary>
    [StringLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// Thumbnail изображения в base64 (только для изображений).
    /// </summary>
    public string? Thumb { get; set; }

    /// <summary>
    /// MIME тип файла.
    /// </summary>
    [StringLength(100)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Размер файла в байтах.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Дата и время прочтения сообщения.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Исходный JSON сообщения с канала (аудит).</summary>
    [Column(TypeName = "jsonb")]
    public string? RawJson { get; set; }

    #region EF

    /// <summary>
    /// ID Диалога, к которому относится сообщение.
    /// </summary>
    public int ConversationId { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public ConversationEntity Conversation { get; set; } = null!;

    #endregion
}
