using Ac.Domain.ValueObjects;

namespace Ac.Application.Contracts.Models;

public record OutboundMessage(
    /// <summary>Адрес доставки в канале (chat_id, client_id и т.д.).</summary>
    string ChatId,
    ReplyIntent Intent,
    ChannelContext ChannelContext);
