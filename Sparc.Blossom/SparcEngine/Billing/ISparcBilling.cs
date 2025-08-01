﻿using Refit;
using Sparc.Core.Billing;

namespace Sparc.Engine.Billing;

public interface ISparcBilling
{
    [Post("/billing/payments")]
    Task<SparcPaymentIntent> StartCheckoutAsync([Body] SparcOrder order);

    [Get("/billing/products/{productId}")]
    Task<GetProductResponse> GetProductAsync(string productId, string? currency = null);

    [Get("/billing/orders/{id}")]
    Task<SparcOrder> GetOrderAsync(string id);

    [Get("/billing/currencies")]
    Task<List<SparcCurrency>> GetCurrenciesAsync();
}
