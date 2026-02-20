using Ac.Application.Interfaces;
using Ac.Domain.Entities;

namespace Ac.Data.Repositories;

public class MessageRepository(ApiDbContext db) : IMessageRepository
{
    public async Task AddAsync(MessageEntity message, CancellationToken ct = default)
        => await db.Messages.AddAsync(message, ct);
}
