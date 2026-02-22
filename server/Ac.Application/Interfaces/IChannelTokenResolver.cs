using Ac.Application.Models;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Interfaces;

public interface IChannelTokenResolver
{
    Task<ChannelContext?> ResolveAsync(ChannelToken token, CancellationToken ct = default);
}
