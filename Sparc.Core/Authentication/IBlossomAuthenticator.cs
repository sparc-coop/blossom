using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    SparcUser? User { get; }
    public string? Message { get; set; }


    Task<SparcUser> GetAsync(ClaimsPrincipal principal);
    Task<SparcUser> UpdateAsync(ClaimsPrincipal principal, SparcUser avatar);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);
}