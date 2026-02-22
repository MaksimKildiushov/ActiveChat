using Ac.Domain.Enums;

namespace Ac.Application.Models;

public record ChannelContext(
    int ChannelId,
    int TenantId,
    ChannelType ChannelType,
    string? SettingsJson);
