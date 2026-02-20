using Ac.Application.Interfaces;
using Ac.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ac.Data.Repositories;

public class ConversationRepository(ApiDbContext db) : IConversationRepository
{
    public Task<ConversationEntity?> FindAsync(
        Guid tenantId, Guid channelId, string externalUserId, CancellationToken ct = default)
        => db.Conversations.FirstOrDefaultAsync(
            c => c.TenantId == tenantId
                 && c.ChannelId == channelId
                 && c.ExternalUserId == externalUserId,
            ct);

    public async Task<ConversationEntity> CreateAsync(ConversationEntity conversation, CancellationToken ct = default)
    {
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }
}
