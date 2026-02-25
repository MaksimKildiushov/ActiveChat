using Hangfire.Dashboard;

namespace Ac.Admin.Infrastructure;

/// <summary>
/// Разрешает доступ к дашборду Hangfire только пользователям в роли Admin.
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string AdminRole = "Admin";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole(AdminRole);
    }
}
