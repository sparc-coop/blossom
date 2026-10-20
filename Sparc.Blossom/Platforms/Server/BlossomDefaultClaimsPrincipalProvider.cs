using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom.Platforms.Server;

internal class BlossomDefaultClaimsPrincipalProvider : IClaimsPrincipalProvider
{
    public ClaimsPrincipal Principal => new(new ClaimsIdentity());
}