using Ac.Data.Repositories;
using Ac.Domain.Entities;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Services;

public class ClientService(ClientRepository clients)
{
    /// <summary>
    /// Находит клиента по приоритету: OverrideUserId → ChannelUserId → Email → Phone.
    /// Если не найден — создаёт клиента и заполняет все переданные поля.
    /// </summary>
    public async Task<ClientEntity> GetOrCreateAsync(
        ChannelContext channelCtx,
        string? channelUserId,
        string? overrideUserId,
        string? email,
        string? phone,
        string? displayName = null,
        string? traceId = null,
        string? timezone = null,
        string? metadataJson = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(overrideUserId))
        {
            var byOverride = await clients.FindByOverrideUserIdAsync(overrideUserId, ct);
            if (byOverride is not null)
                return byOverride;
        }

        if (!string.IsNullOrWhiteSpace(channelUserId))
        {
            var byChannel = await clients.FindByChannelUserIdAsync(channelUserId, ct);
            if (byChannel is not null)
                return byChannel;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = await clients.FindByEmailAsync(email, ct);
            if (byEmail is not null)
                return byEmail;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var byPhone = await clients.FindByPhoneAsync(phone, ct);
            if (byPhone is not null)
                return byPhone;
        }

        var client = new ClientEntity
        {
            ChannelUserId = channelUserId,
            OverrideUserId = overrideUserId,
            DisplayName = displayName,
            Email = email,
            Phone = phone,
            TraceId = traceId,
            Timezone = timezone,
            MetadataJson = metadataJson,
        };

        return await clients.CreateAsync(client, ct);
    }
}
