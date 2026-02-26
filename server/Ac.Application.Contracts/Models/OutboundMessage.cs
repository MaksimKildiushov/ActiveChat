using Ac.Domain.ValueObjects;

namespace Ac.Application.Contracts.Models;

/// <summary>
/// Ответ оператора или AI пользователю.
/// </summary>
/// <param name="ChatId">ID чата в канале (куда отправлять ответ).</param>
/// <param name="Intent"></param>
/// <param name="ChannelContext"></param>
/// <param name="ClientId">ID пользователя в канале (client_id в Jivo, from.id в Telegram и т.д.) — приходит во входящем сообщении.</param>
public record OutboundMessage(string ChatId, ReplyIntent Intent, ChannelContext ChannelContext, string ClientId);
