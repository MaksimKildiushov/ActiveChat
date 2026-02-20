using Ac.Domain.Enums;

namespace Ac.Application.Models;

public record ChannelContext(
    Guid ChannelId,
    Guid TenantId,
    ChannelType ChannelType,
    string? SettingsJson);
