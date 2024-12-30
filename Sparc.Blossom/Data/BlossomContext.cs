using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public class BlossomContext(IHttpContextAccessor http)
{
    public string UserId => http?.HttpContext?.User?.Identity?.IsAuthenticated == true ? http.HttpContext.User.Id() : "anonymous";
}