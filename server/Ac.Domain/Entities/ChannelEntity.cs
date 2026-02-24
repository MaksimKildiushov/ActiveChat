using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ac.Domain.Enums;
using Ac.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Ac.Domain.Entities;

/// <summary>Канал подключения (Jivo, Telegram и т.д.) у тенанта. Таблица в public.</summary>
[Table("Channels")]
[Index(nameof(Token), IsUnique = true)]
public class ChannelEntity : IntEntity
{
    /// <summary>Тип канала (Jivo, Telegram, Webhook и т.д.).</summary>
    [MaxLength(64)]
    public ChannelType ChannelType { get; set; }

    /// <summary>Токен канала для вебхука (идентификация в URL).</summary>
    public Guid Token { get; set; }

    /// <summary>Настройки канала в JSON.</summary>
    [Column(TypeName = "jsonb")]
    public string? SettingsJson { get; set; }

    /// <summary>Канал активен и принимает сообщения.</summary>
    public bool IsActive { get; set; }

    /// <summary>Тенант-владелец канала.</summary>
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public TenantEntity Tenant { get; set; } = null!;
}
