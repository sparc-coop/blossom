using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    BlossomUser? GenericUser { get; }
    LoginStates LoginState { get; set; }
    public string? Message { get; set; }


    Task<BlossomUser> GetGenericAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal);
    Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId);
    Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal);

    IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal? principal, string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal? principal);
}

public interface IBlossomAuthenticator<T> where T : BlossomUser
{
    T? User { get; }
    Task<T> GetAsync(ClaimsPrincipal principal);
}
