using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Stripe;
using Sparc.Blossom.Payment.Stripe;
using Sparc.Blossom.Billing;

namespace Sparc.Blossom.Cloud.Billing
{
    public record CreateOrderPaymentRequest(
        long Amount,
        string Currency,
        string? CustomerId,
        string? ReceiptEmail,
        Dictionary<string, string>? Metadata,
        string? SetupFutureUsage
    );

    public record ConfirmOrderPaymentRequest(
        string PaymentIntentId,
        string PaymentMethodId
    );

    public class BlossomBillingService : StripePaymentService, IBlossomCloudApi
    {
        public BlossomBillingService(
            IOptions<Payment.Stripe.StripeClientOptions> options,
            ExchangeRates rates
        ) : base(options, rates)
        {
        }

        public async Task<string> CreateOrderPaymentAsync(
            long orderAmount,
            string orderCurrency,
            string? customerId = null,
            string? receiptEmail = null,
            Dictionary<string, string>? metadata = null,
            string? setupFutureUsage = null
        )
        {
            var paymentIntent = await CreatePaymentIntentAsync(
                amount: orderAmount,
                currency: orderCurrency,
                customerId: customerId,
                receiptEmail: receiptEmail,
                metadata: metadata,
                setupFutureUsage: setupFutureUsage
            );

            return paymentIntent.ClientSecret;
        }

        public async Task<PaymentIntent> ConfirmOrderPaymentAsync(
            string paymentIntentId,
            string paymentMethodId
        )
        {
            return await ConfirmPaymentIntentAsync(
                paymentIntentId: paymentIntentId,
                paymentMethodId: paymentMethodId
            );
        }

        public async Task<GetProductResponse> GetProductAsync(string productId)
        {
            var product = await base.GetProductAsync(productId);
            var priceList = await GetAllPricesForProductAsync(productId);

            var priceResultList = new List<Dictionary<string,long>>();

            foreach (var price in priceList)
            {
                var prices = new Dictionary<string, long>();
                foreach (var item in price.CurrencyOptions)
                {
                    prices.Add(item.Key, item.Value.UnitAmount ?? 0);
                    
                }
                priceResultList.Add(prices);
            }

            var result = new GetProductResponse
            (
                Id: product.Id,
                Name: product.Name,
                Price: 0,
                Currency: "usd",
                IsActive: product.Active,
                Prices: priceResultList
            );

            return result;
        }

        public void Map(IEndpointRouteBuilder endpoints)
        {
            var billingGroup = endpoints.MapGroup("/billing");

            billingGroup.MapPost("/create-order-payment",
                async (BlossomBillingService svc, CreateOrderPaymentRequest req) =>
                {
                    var clientSecret = await svc.CreateOrderPaymentAsync(
                        orderAmount: req.Amount,
                        orderCurrency: req.Currency,
                        customerId: req.CustomerId,
                        receiptEmail: req.ReceiptEmail,
                        metadata: req.Metadata,
                        setupFutureUsage: req.SetupFutureUsage
                    );

                    return Results.Ok(new { clientSecret });
                });

            billingGroup.MapPost("/confirm-order-payment",
                async (BlossomBillingService svc, ConfirmOrderPaymentRequest req) =>
                {
                    var confirmedIntent = await svc.ConfirmOrderPaymentAsync(
                        paymentIntentId: req.PaymentIntentId,
                        paymentMethodId: req.PaymentMethodId
                    );

                    return Results.Ok(confirmedIntent);
                });

            billingGroup.MapGet("/get-product/{productId}",
                async (BlossomBillingService svc, string productId) =>
                {
                    var product = await svc.GetProductAsync(productId);
                    return Results.Ok(product);
                });
        }
    }
}
