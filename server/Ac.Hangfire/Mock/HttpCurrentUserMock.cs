using Ac.Abstractions.Helpers;
using Ac.Data.Accessors;

namespace Ac.Hangfire.Mock
{
    public class HttpCurrentUserMock : ICurrentUser
    {
        public Guid UserId => SysAccountsHlp.Job.Id;
    }
}
