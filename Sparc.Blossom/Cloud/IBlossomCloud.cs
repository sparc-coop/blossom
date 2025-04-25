using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public interface IBlossomCloud
{
    /// <returns>OK</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: text/plain")]
    [Get("/tools/friendlyid")]
    Task<string> FriendlyId();

    [Get("/auth/login")]
    Task<LoginStates> Login(string? emailOrToken = null);

    [Get("/auth/userinfo")]
    Task<BlossomUser> UserInfo();
}