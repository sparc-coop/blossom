using Refit;
using Sparc.Core.Billing;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<SparcPaymentIntent> StartCheckoutAsync([Body] SparcOrder order);

    [Get("/billing/products/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId, string? currency = null);

    [Get("/billing/currencies")]
    Task<List<SparcCurrency>> GetCurrenciesAsync();

    [Get("/billing/currency")]
    Task<SparcCurrency?> GetUserCurrencyAsync();

    [Post("/billing/currency")]
    Task<SparcCurrency> SetUserCurrencyAsync([Body] SparcCurrency currency);
}
