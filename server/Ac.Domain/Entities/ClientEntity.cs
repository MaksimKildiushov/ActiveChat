using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

/// <summary>
/// Клиент — тот, кто обратился в чат; пользователь, с которым ведётся диалог.
/// Идентификация по приоритету: OverrideUserId → ChannelUserId → Email → Phone.
/// </summary>
[Table("Clients")]
[Index(nameof(ChannelUserId), IsUnique = true)]
public class ClientEntity : IntEntity
{
    /// <summary>Уникальный идентификатор пользователя в канале (в рамках тенанта).</summary>
    [MaxLength(256)]
    public string? ChannelUserId { get; set; }

    /// <summary>UserId, заданный тенантом; при наличии переопределяет использование остальных полей клиента.</summary>
    [MaxLength(256)]
    public string? OverrideUserId { get; set; }

    /// <summary>Как обращаться к клиенту (имя для отображения).</summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>Адрес электронной почты клиента.</summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>Номер телефона клиента.</summary>
    [MaxLength(64)]
    public string? Phone { get; set; }

    /// <summary>Идентификатор трассировки (например OpenTelemetry trace id) для связи обращений.</summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>Часовой пояс клиента (например "Europe/Moscow", IANA).</summary>
    [MaxLength(64)]
    public string? Timezone { get; set; }

    /// <summary>Сырые данные с канала (username, display name и т.д.) в JSON.</summary>
    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    /// <summary>Дополнительные поля из опросников, заполняемые для клиента (JSON).</summary>
    [Column(TypeName = "jsonb")]
    public string? AdditionalFieldsJson { get; set; }

    /// <summary>Признак блокировки клиента (не принимать сообщения).</summary>
    public bool IsBlocked { get; set; }
}
