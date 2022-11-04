using System.Security.Claims;

namespace Sparc.Authentication;

public interface ISparcUser
{
    public string? SecurityStamp { get; set; }

    public string? UserName { get; set; }

    public string? LoginProviderKey { get; set; }

    public ClaimsPrincipal CreatePrincipal();
}
