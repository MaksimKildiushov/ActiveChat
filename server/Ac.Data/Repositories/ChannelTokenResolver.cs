using Ac.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ac.Data.Repositories;

public class ChannelTokenResolver(ApiDb db)
{
    public async Task<ChannelContext?> ResolveAsync(ChannelToken token, CancellationToken ct = default)
    {
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Token == token.Value && c.IsActive)
            .Select(c => new { c.Id, c.TenantId, c.Tenant.Inn, c.ChannelType, c.SettingsJson })
            .FirstOrDefaultAsync(ct);

        return channel is null
            ? null
            : new ChannelContext(channel.Id, channel.TenantId, "t_" + channel.Inn, channel.ChannelType, channel.SettingsJson);
    }
}
