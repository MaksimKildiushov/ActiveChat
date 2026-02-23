using Ac.Application.Contracts.Interfaces;
using Ac.Domain.Enums;

namespace Ac.Application.Services;

public class InboundParserRegistry(IEnumerable<IInboundParser> parsers)
{
    private readonly Dictionary<ChannelType, IInboundParser> _parsers = parsers.ToDictionary(p => p.ChannelType);

    public IInboundParser GetParser(ChannelType channelType)
        => _parsers.TryGetValue(channelType, out var parser)
            ? parser
            : throw new InvalidOperationException(
                $"No parser registered for ChannelType '{channelType}'. Register IInboundParser implementation.");
}
