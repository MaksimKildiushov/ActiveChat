using Ac.Domain.Enums;

namespace Ac.Application.Models;

public record ChannelContext(
    Guid ChannelId,
    int TenantId,
    ChannelType ChannelType,
    string? SettingsJson);
