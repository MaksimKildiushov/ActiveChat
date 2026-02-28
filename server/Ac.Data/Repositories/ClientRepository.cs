using Ac.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ac.Data.Repositories;

public class ClientRepository(TenantDb db)
{
    public Task<ClientEntity?> FindByOverrideUserIdAsync(string overrideUserId, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.OverrideUserId == overrideUserId, ct);

    public Task<ClientEntity?> FindByChannelUserIdAsync(string channelUserId, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.ChannelUserId == channelUserId, ct);

    public Task<ClientEntity?> FindByEmailAsync(string email, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.Email != null && c.Email == email, ct);

    public Task<ClientEntity?> FindByPhoneAsync(string phone, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.Phone != null && c.Phone == phone, ct);

    public Task<ClientEntity?> FindByEmailAndPhoneAsync(string email, string? phone, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(
            c => c.Email != null && c.Email == email && c.Phone == phone, ct);

    public async Task<ClientEntity> CreateAsync(ClientEntity client, CancellationToken ct = default)
    {
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client;
    }
}
