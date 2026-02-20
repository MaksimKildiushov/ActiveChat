using Ac.Domain.Entities;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Models;

public record DecisionContext(
    ConversationEntity Conversation,
    UnifiedInboundMessage InboundMessage,
    ChannelContext ChannelContext);
