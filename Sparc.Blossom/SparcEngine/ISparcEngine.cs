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

    [Post("/auth/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] BlossomAvatar avatar);

    [Get("/tovik/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);

    //[Post("/billing/create-order-payment")]
    //Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);

    //[Get("/billing/get-product/{productId}")]
    //Task<GetProductResponse> GetProductAsync(string productId);

}