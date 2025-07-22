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

    [Post("/aura/register")]
    Task<SparcCode> Register();

    [Post("/aura/login")]
    Task<BlossomLogin> Login(string? emailOrToken = null);

    [Post("/aura/logout")]
    Task<BlossomUser> Logout();

    [Get("/aura/code")]
    Task<SparcCode?> GetSparcCode();

    [Get("/aura/userinfo")]
    Task<BlossomUser> UserInfo();

    [Post("/aura/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] BlossomAvatar avatar);
}
