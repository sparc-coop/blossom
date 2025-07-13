using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public interface ISparcAura
{
    [Get("/hi")]
    Task<SparcAura> Hi();

    [Get("/me")]
    Task<SparcAura> GetUserInfo();

    [Patch("/me")]
    Task<SparcAura> UpdateUserInfo([Body] SparcAura avatar);
}

    public interface ITovik
{
    [Get("/tovik/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<SparcAura> AddUserLanguage([Body] Language language);

    //[Post("/billing/create-order-payment")]
    //Task<CreateOrderPaymentResponse> CreateOrderPaymentAsync([Body] CreateOrderPaymentRequest request);

    //[Get("/billing/get-product/{productId}")]
    //Task<GetProductResponse> GetProductAsync(string productId);
}