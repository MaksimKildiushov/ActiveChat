using Ac.Application.Interfaces;
using Ac.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Ac.Data.Repositories;

public class ChannelTokenResolver(ApiDbContext db) : IChannelTokenResolver
{
    public async Task<ChannelContext?> ResolveAsync(string token, CancellationToken ct = default)
    {
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Token == token && c.IsActive)
            .Select(c => new { c.Id, c.TenantId, c.ChannelType, c.SettingsJson })
            .FirstOrDefaultAsync(ct);

        return channel is null
            ? null
            : new ChannelContext(channel.Id, channel.TenantId, channel.ChannelType, channel.SettingsJson);
    }
}
