using Ac.Abstractions.Helpers;
using System.Security.Claims;

namespace Ac.Hangfire.Mock
{
    public class ClaimsPrincipalMock
    {
        public static ClaimsPrincipal BackEndService
        {
            get
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, SysAccountsHlp.Job.Id.ToString("D")),
                    new(ClaimTypes.Name, SysAccountsHlp.Job.Name),
                    new(ClaimTypes.Email, SysAccountsHlp.Job.Name),
                    new("AspNet.Identity.SecurityStamp", "005311A0DA3842DFA071FFAD95A003AE")
                };

                ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie",
                    ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

                //var principal = new ClaimsPrincipal(new ClaimsIdentity(null, "Basic"));
                var principal = new ClaimsPrincipal(id);
                //var isAuthenticated = principal.Identity.IsAuthenticated; // true

                return principal;
            }
        }
    }
}
