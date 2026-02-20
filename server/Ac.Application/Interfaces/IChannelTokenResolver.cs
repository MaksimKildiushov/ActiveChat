using Ac.Application.Models;

namespace Ac.Application.Interfaces;

public interface IChannelTokenResolver
{
    Task<ChannelContext?> ResolveAsync(string token, CancellationToken ct = default);
}
