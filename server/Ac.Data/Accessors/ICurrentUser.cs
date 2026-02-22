using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Ac.Data.Accessors;

public interface ICurrentUser
{
    Guid UserId { get; }
}

/// <summary>
/// Позволяет явно выставить текущего пользователя.
/// Используется в Blazor Server, где HttpContext недоступен во время SignalR-вызовов.
/// </summary>
public interface ICurrentUserSetter
{
    void SetCurrentUser(Guid userId);
}

/// <summary>
/// Скоупированная реализация для Blazor Server (per-circuit).
/// Инициализируется через ICurrentUserSetter в MainLayout.
/// </summary>
public sealed class ScopedCurrentUser : ICurrentUser, ICurrentUserSetter
{
    private Guid? _userId;
    public Guid UserId => _userId ?? throw new InvalidOperationException("Текущий пользователь не инициализирован.");
    public void SetCurrentUser(Guid userId) => _userId = userId;
}

/// <summary>
/// HTTP-реализация для API — читает user ID из JWT-клейма.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var ctx = accessor.HttpContext;
            var raw = ctx?.User?.FindFirst("sub")?.Value
                   ?? ctx?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var id))
                throw new InvalidOperationException("User ID claim not found in token.");

            return id;
        }
    }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
