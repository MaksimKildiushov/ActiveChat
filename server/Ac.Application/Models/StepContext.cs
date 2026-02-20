using Ac.Domain.Entities;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Models;

public record StepContext(
    ConversationEntity Conversation,
    UnifiedInboundMessage InboundMessage,
    DecisionResult DecisionResult,
    ChannelContext ChannelContext);
