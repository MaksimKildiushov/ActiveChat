using Ac.Domain.Enums;

namespace Ac.Domain.ValueObjects;

public record ChannelContext(
    int ChannelId,
    int TenantId,
    string SchemaName,
    ChannelType ChannelType,
    string? SettingsJson);
