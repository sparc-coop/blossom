using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    BlossomUser? User { get; }

    Task<BlossomUser?> GetAsync(ClaimsPrincipal principal);
    Task<BlossomUser> GetAsync(string username);
    IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null);
    Task<BlossomUser?> LoginWithTokenAsync(string token);
    IAsyncEnumerable<LoginStates> LogoutAsync();
    Task<bool> SendMagicLinkAsync(string emailAddress, string urlTemplate, string userId, int timeToLive = 3600);
}