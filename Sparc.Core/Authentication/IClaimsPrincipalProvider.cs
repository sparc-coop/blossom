using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IClaimsPrincipalProvider
{
    ClaimsPrincipal Principal { get; }
}
