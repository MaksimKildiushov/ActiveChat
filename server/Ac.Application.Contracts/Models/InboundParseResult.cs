using Ac.Domain.ValueObjects;

namespace Ac.Application.Contracts.Models;

public enum InboundParseStatus
{
    Message,
    ChatClosed
}

/// <summary>Результат парсинга входящего запроса канала.</summary>
public sealed record InboundParseResult(
    InboundParseStatus Status,
    UnifiedInboundMessage? Message);

