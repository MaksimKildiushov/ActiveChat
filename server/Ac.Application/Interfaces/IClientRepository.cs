using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IClientRepository
{
    Task<ClientEntity?> FindByOverrideUserIdAsync(string overrideUserId, CancellationToken ct = default);
    Task<ClientEntity?> FindByChannelUserIdAsync(string channelUserId, CancellationToken ct = default);
    Task<ClientEntity?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<ClientEntity?> FindByPhoneAsync(string phone, CancellationToken ct = default);
    Task<ClientEntity> CreateAsync(ClientEntity client, CancellationToken ct = default);
}
