using Microsoft.AspNetCore.Identity;
using Sparc.Kernel;
using System.Security.Claims;

namespace Sparc.Authentication;

public class SparcUser : Root<string>
{
    public string? SecurityStamp
    {
        get; set;
    }

    public string? UserName { get; set; }

    internal ClaimsPrincipal CreatePrincipal()
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>
                {
                    new Claim("sub", Id)
                }, IdentityConstants.ApplicationScheme
                ));
    }

    
}
