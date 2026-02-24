using Ac.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities;

/// <summary>
/// Диалог в канале. Привязан к клиенту (с кем общаемся); один диалог на пару канал + клиент.
/// Тенант задаётся схемой БД, отдельное поле TenantId не требуется.
/// </summary>
[Table("Conversations")]
[Index(nameof(ChannelId), nameof(ClientId), IsUnique = true)]
public class ConversationEntity : IntEntity
{
    /// <summary>Идентификатор чата в канале — куда отправлять ответы.</summary>
    [MaxLength(256)]
    public string? ChatId { get; set; }

    /// <summary>Состояние диалога (следующий шаг, параметры), как краткий кэш.</summary>
    [Column(TypeName = "jsonb")]
    public string? StateJson { get; set; }

    /// <summary>Канал, в котором идёт диалог.</summary>
    public int ChannelId { get; set; }

    [ForeignKey(nameof(ChannelId))]
    public ChannelEntity Channel { get; set; } = null!;

    /// <summary>Клиент (обращающийся), определённый по приоритету OverrideUserId → ChannelUserId → Email → Phone.</summary>
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public ClientEntity Client { get; set; } = null!;

    /// <summary>Сообщения в диалоге.</summary>
    public ICollection<MessageEntity> Messages { get; set; } = [];

    /// <summary>Аудит решений ИИ по шагам диалога.</summary>
    public ICollection<DecisionAuditEntity> DecisionAudits { get; set; } = [];
}
