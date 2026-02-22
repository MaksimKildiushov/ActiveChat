using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Ac.Data.Accessors;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            // Сначала пробуем "sub" (стандартный JWT claim)
            var userId = accessor.HttpContext?.User?.FindFirst("sub")?.Value;
            
            // Если нет, пробуем NameIdentifier (для совместимости)
            if (string.IsNullOrEmpty(userId))
            {
                userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("User ID claim not found in token");
                }
                
                return new Guid(userId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось получить UID пользователя. sub='{userId}' (error 1718051025).", ex);
            }
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
