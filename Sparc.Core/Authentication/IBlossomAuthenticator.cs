using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    BlossomUser? User { get; }
    public string? Message { get; set; }


    Task<BlossomUser> GetAsync(ClaimsPrincipal principal);
    Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, UserAvatar avatar);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);
    Task<BlossomUser> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal principal, string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal);
}