using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    BlossomUser? Principal { get; set; }
    LoginStates LoginState { get; set; }
    public string? Message { get; set; }


    Task<BlossomUser> GetAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);

    IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal? principal, string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal? principal);

}

public interface IBlossomAuthenticator<T> : IBlossomAuthenticator
    where T : BlossomUser, new()
{
    T? User { get; }
}