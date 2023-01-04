using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IUser
{
    public string? SecurityStamp { get; set; }

    public string? UserName { get; set; }

    public string? LoginProviderKey { get; set; }

    public ClaimsPrincipal CreatePrincipal();
}
