using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomClaimsPrincipalProvider(IHttpContextAccessor accessor)
{
    public IHttpContextAccessor Accessor { get; } = accessor;
    public ClaimsPrincipal Principal => Accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
}
