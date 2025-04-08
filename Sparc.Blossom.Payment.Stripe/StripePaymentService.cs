using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparc.Blossom.Payment.Stripe
{
    public class StripePaymentService
    {
        private readonly StripeClientOptions _options;

        public StripePaymentService(IOptions<StripeClientOptions> options)
        {
            _options = options.Value;

            StripeConfiguration.ApiKey = _options.ApiKey;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(
            long amount,
            string currency,
            string? customerId = null,
            string? receiptEmail = null,
            Dictionary<string, string>? metadata = null)
        {
            var service = new PaymentIntentService();
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = currency,
                Customer = customerId,
                ReceiptEmail = receiptEmail,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = metadata,
            };

            return await service.CreateAsync(createOptions);
        }

        public async Task<PaymentIntent> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            string paymentMethodId)
        {
            var service = new PaymentIntentService();
            var confirmOptions = new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethodId
            };
            return await service.ConfirmAsync(paymentIntentId, confirmOptions);
        }
    }
}
