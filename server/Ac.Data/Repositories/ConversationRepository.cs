using Ac.Application.Interfaces;
using Ac.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ac.Data.Repositories;

public class ConversationRepository(TenantDb db) : IConversationRepository
{
    public Task<ConversationEntity?> FindAsync(
        int channelId, int clientId, CancellationToken ct = default)
        => db.Conversations.FirstOrDefaultAsync(
            c => c.ChannelId == channelId && c.ClientId == clientId,
            ct);

    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default)
    {
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }
}
