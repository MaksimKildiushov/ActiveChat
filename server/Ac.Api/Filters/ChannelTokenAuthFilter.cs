using System.Security.Claims;
using Ac.Data;
using Ac.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Ac.Api.Filters;

/// <summary>
/// Анонимный endpoint, но с импersonацией пользователя тенанта по ChannelToken.
/// Устанавливает HttpContext.User → ICurrentUser → AuditingInterceptor корректно пишет AuthorId.
/// Результаты резолва кешируются на 5 минут (IDistributedCache) для снижения нагрузки на БД.
/// </summary>
public sealed class ChannelTokenAuthFilter(ApiDb db, IDistributedCache cache) : IAsyncActionFilter
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ct = context.HttpContext.RequestAborted;

        if (!context.ActionArguments.TryGetValue("channelToken", out var raw) || raw is not Guid tokenGuid)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Channel token missing." });
            return;
        }

        var userId = await ResolveUserIdAsync(tokenGuid, ct);

        if (userId == Guid.Empty)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or inactive channel token." });
            return;
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ChannelToken"));

        await next();
    }

    /// <summary>
    /// Возвращает ID пользователя-Owner тенанта для данного channel-токена.
    /// Guid.Empty — если канал не найден, неактивен или у тенанта нет Owner.
    /// </summary>
    private async Task<Guid> ResolveUserIdAsync(Guid token, CancellationToken ct)
    {
        var cacheKey = $"ch:uid:{token:N}";

        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is { Length: 16 })
            return new Guid(cached);

        var userId = await db.Channels
            .AsNoTracking()
            .Where(c => c.Token == token && c.IsActive)
            .Join(
                db.TenantUsers.Where(tu => tu.Role == TenantRole.Owner),
                c => c.TenantId,
                tu => tu.TenantId,
                (_, tu) => tu.UserId)
            .FirstOrDefaultAsync(ct);

        if (userId != Guid.Empty)
            await cache.SetAsync(cacheKey, userId.ToByteArray(), CacheOptions, ct);

        return userId;
    }
}
