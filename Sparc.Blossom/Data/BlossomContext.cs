using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom;

public class BlossomContext(ClaimsPrincipal principal)
{
    public string UserId => Principal.Identity?.IsAuthenticated == true ? Principal.Id() : "anonymous";

    ClaimsPrincipal Principal { get; } = principal;
}