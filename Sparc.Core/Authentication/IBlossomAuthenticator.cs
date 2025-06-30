using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    BlossomUser? User { get; }
    public string? Message { get; set; }


    Task<BlossomUser> GetAsync(ClaimsPrincipal principal);
    Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);
}