using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;

namespace Ac.Application.Contracts.Interfaces;

public interface IChannelDeliveryAdapter
{
    ChannelType ChannelType { get; }
    Task DeliverAsync(OutboundMessage message, CancellationToken ct = default);
}
