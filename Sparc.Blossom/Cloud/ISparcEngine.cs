using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;

namespace Sparc.Engine;

public interface ISparcEngine
{
    /// <returns>OK</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: text/plain")]
    [Get("/tools/friendlyid")]
    Task<string> FriendlyId();

    [Headers("Accept: text/plain")]
    [Get("/tools/friendlyusername")]
    Task<string> FriendlyUsername();

    [Get("/auth/login")]
    Task<BlossomUser> Login(string? emailOrToken = null);

    [Get("/auth/userinfo")]
    Task<BlossomUser> UserInfo();

    [Get("/translate/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/userinfo")]
    Task<BlossomUser> UpdateUserInfo([Body] UserAvatar userInfo);

    [Post("/user/user-products")]
    Task<BlossomUser> AddUserProduct([Body] AddProductRequest request);

    [Post("/user/update-user")]
    Task<BlossomUser> UpdateUser([Body] UpdateUserRequest request);

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);

    [Post("/billing/create-order-payment")]
    Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);
    [Post("/billing/create-order")]
    Task<CreateOrderPaymentResponse> CreateOrderAsync([Body] CreateOrderRequest request);

    [Get("/billing/get-product/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId);


    //[Post("/billing/confirm-order-payment")]
    //Task<PaymentIntent> ConfirmOrderPaymentAsync([Body] ConfirmOrderPaymentRequest request);

    [Post("/user/verify-code")]
    Task<bool> VerifyCode([Body] VerificationRequest request);

    [Post("/user/update-avatar")]
    Task<BlossomUser> UpdateAvatar([Body] UpdateAvatarRequest request);
}