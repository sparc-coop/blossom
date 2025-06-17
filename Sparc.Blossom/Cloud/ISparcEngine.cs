using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public interface ISparcEngine
{
    /// <returns>OK</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: text/plain")]
    [Get("/tools/friendlyid")]
    Task<string> FriendlyId();

    [Get("/auth/login")]
    Task<BlossomUser> Login(string? emailOrToken = null);

    [Get("/auth/userinfo")]
    Task<BlossomUser> UserInfo();

    [Get("/translate/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] UserAvatar userInfo);

    [Post("/auth/user-products")]
    Task<BlossomUser> AddUserProduct([Body] AddProductRequest request);

}