using Refit;

namespace Sparc.Blossom.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<SparcPaymentIntent> StartCheckoutAsync([Body] StartCheckoutRequest request);

    [Get("/billing/products/{productId}")]
    Task<SparcProduct> GetProductAsync(string productId, string? currency = null);

    [Get("/billing/orders/{id}")]
    Task<SparcOrder> GetOrderAsync(string id);

    [Get("/billing/currencies")]
    Task<List<SparcCurrency>> GetCurrenciesAsync();
}

public record StartCheckoutRequest(string Domain, string ProductId, string TierId, string? Currency, string? PaymentIntentId)
{
    public string? Email { get; set; }
};