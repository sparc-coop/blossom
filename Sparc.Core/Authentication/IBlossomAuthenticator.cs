using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    SparcAura? User { get; }
    public string? Message { get; set; }


    Task<SparcAura> GetAsync(ClaimsPrincipal principal);
    Task<SparcAura> UpdateAsync(ClaimsPrincipal principal, SparcAura avatar);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);
}