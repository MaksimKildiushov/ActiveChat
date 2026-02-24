using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IConversationRepository
{
    Task<ConversationEntity?> FindAsync(int channelId, int clientId, CancellationToken ct = default);
    Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default);
}
