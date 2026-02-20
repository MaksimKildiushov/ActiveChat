using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IConversationRepository
{
    Task<ConversationEntity?> FindAsync(Guid tenantId, Guid channelId, string externalUserId, CancellationToken ct = default);
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default);
}
