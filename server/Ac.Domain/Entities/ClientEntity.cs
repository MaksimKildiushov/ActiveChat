using Ac.Domain.Entities.Abstract;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

/// <summary>
/// Клиент — тот, кто обратился в чат; пользователь, с которым ведётся диалог.
/// Идентификация по приоритету: OverrideUserId → Email (+ Phone) → ChannelUserId.
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

    /// <summary>
    /// Дополнительные поля клиента как словарь ключ/значение.
    /// Хранится в БД в виде JSON в <see cref="AdditionalFieldsJson"/>.
    /// </summary>
    [NotMapped]
    public IDictionary<string, string> AdditionalFields
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AdditionalFieldsJson))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(AdditionalFieldsJson);
                return dict is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // В случае некорректного JSON не падаем, а возвращаем пустой словарь.
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        set
        {
            if (value is null || value.Count == 0)
            {
                AdditionalFieldsJson = null;
                return;
            }

            // Не удаляем существующие поля при обновлении словаря — только дополняем/перезаписываем.
            var current = new Dictionary<string, string>(AdditionalFields, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in value)
                current[pair.Key] = pair.Value;

            var ordered = new SortedDictionary<string, string>(current, StringComparer.OrdinalIgnoreCase);
            AdditionalFieldsJson = JsonSerializer.Serialize(ordered);
        }
    }

    /// <summary>
    /// Устанавливает дополнительное поле, не удаляя остальные.
    /// При value = null или пустой строке поле удаляется.
    /// </summary>
    public void SetAdditionalField(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var dict = new Dictionary<string, string>(AdditionalFields, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(value))
            dict.Remove(key);
        else
            dict[key] = value;

        if (dict.Count == 0)
        {
            AdditionalFieldsJson = null;
            return;
        }

        var ordered = new SortedDictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        AdditionalFieldsJson = JsonSerializer.Serialize(ordered);
    }

    /// <summary>Признак блокировки клиента (не принимать сообщения).</summary>
    public bool IsBlocked { get; set; }
}
