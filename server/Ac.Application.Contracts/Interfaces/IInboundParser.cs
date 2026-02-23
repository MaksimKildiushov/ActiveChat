using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Contracts.Interfaces;

public interface IInboundParser
{
    ChannelType ChannelType { get; }

    UnifiedInboundMessage Parse(string rawJson);
}
