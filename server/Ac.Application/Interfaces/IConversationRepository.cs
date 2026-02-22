using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IConversationRepository
{
    Task<ConversationEntity?> FindAsync(int tenantId, int channelId, string externalUserId, CancellationToken ct = default);
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default);
}
