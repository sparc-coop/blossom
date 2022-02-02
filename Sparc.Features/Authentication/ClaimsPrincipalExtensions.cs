using System.Security.Claims;

namespace Sparc.Features
{
    public static class ClaimsPrincipalExtensions
    {
        public static string Id(this ClaimsPrincipal principal) => principal.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(x => x.Type == "sub")?.Value
            ?? string.Empty;
    }
}
