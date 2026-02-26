using Ac.Application.Contracts.Interfaces;
using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;

namespace Ac.Application.Services;

public class IntentDispatcher(IEnumerable<IChannelDeliveryAdapter> adapters)
{
    private readonly Dictionary<ChannelType, IChannelDeliveryAdapter> _adapters = adapters.ToDictionary(a => a.ChannelType);

    public Task DeliverAsync(OutboundMessage message, CancellationToken ct = default)
        => _adapters.TryGetValue(message.ChannelContext.ChannelType, out var adapter)
            ? adapter.DeliverAsync(message, ct)
            : throw new InvalidOperationException(
                $"No adapter registered for ChannelType '{message.ChannelContext.ChannelType}'. Register IChannelDeliveryAdapter implementation.");
}
