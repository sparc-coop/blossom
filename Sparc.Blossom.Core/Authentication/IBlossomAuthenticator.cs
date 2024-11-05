using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    BlossomUser? User { get; }
    public string? Message { get; set; }


    Task<BlossomUser> GetAsync(ClaimsPrincipal principal);
    IAsyncEnumerable<LoginStates> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> LogoutAsync(ClaimsPrincipal principal);
}