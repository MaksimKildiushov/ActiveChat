using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Interfaces;

public interface IInboundParser
{
    ChannelType ChannelType { get; }
    UnifiedInboundMessage Parse(string rawJson);
}
