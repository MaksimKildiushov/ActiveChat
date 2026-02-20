using Ac.Application.Models;
using Ac.Domain.Enums;

namespace Ac.Application.Interfaces;

public interface IChannelDeliveryAdapter
{
    ChannelType ChannelType { get; }
    Task DeliverAsync(OutboundMessage message, CancellationToken ct = default);
}
