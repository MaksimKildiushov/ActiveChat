using Ac.Domain.Entities;

namespace Ac.Application.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(MessageEntity message, CancellationToken ct = default);
}
