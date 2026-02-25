namespace Ac.Hangfire.Mock
{
    public class HttpContextAccessorMock : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = new HttpContextMock();
    }
}
