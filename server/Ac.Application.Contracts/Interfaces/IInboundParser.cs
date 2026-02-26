using Ac.Application.Contracts.Models;
using Ac.Domain.Enums;

namespace Ac.Application.Contracts.Interfaces;

public interface IInboundParser
{
    ChannelType ChannelType { get; }

    InboundParseResult Parse(string rawJson);
}
