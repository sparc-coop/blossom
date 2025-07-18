using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine.Aura;

public interface ISparcAura
{
    /// <returns>OK</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: text/plain")]
    [Get("/aura/friendlyid")]
    Task<string> FriendlyId();

    [Post("/aura/login")]
    Task<BlossomUser> Login(string? emailOrToken = null);

    [Get("/aura/userinfo")]
    Task<BlossomUser> UserInfo();

    [Post("/aura/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] BlossomAvatar avatar);
}
