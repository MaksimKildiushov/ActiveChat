using Ac.Domain.ValueObjects;

namespace Ac.Application.Models;

public record OutboundMessage(
    string ExternalUserId,
    ReplyIntent Intent,
    ChannelContext ChannelContext);
