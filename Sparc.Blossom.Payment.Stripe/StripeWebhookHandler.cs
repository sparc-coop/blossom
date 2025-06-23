using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sparc.Blossom.Payment.StripeIntegration
{
    public class StripeWebhookHandler
    {
        private readonly string _webhookSecret;
        private readonly ILogger<StripeWebhookHandler> _logger;

        public StripeWebhookHandler(string webhookSecret, ILogger<StripeWebhookHandler> logger)
        {
            _webhookSecret = webhookSecret;
            _logger = logger;
        }

        public async Task HandleAsync(HttpRequest request, HttpResponse response)
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    request.Headers["Stripe-Signature"],
                    _webhookSecret
                );
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe signature verification failed.");
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsync("Invalid signature");
                return;
            }

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    // ... do something (log, update DB, etc.)
                    _logger.LogInformation("Payment Intent {Id} succeeded.", paymentIntent?.Id);
                    break;

                case "payment_intent.payment_failed":
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    // ... handle failed payment
                    _logger.LogWarning("Payment Intent {Id} failed.", failedIntent?.Id);
                    break;
            }

            response.StatusCode = StatusCodes.Status200OK;
            await response.WriteAsync("Webhook handled");
        }
    }
    public static class StripeWebhookEndpointExtensions
    {
        public static IEndpointRouteBuilder MapStripeWebhookEndpoint(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Func<HttpContext, StripeWebhookHandler> handlerFactory)
        {
            endpoints.MapPost(pattern, async context =>
            {
                var handler = handlerFactory(context);
                await handler.HandleAsync(context.Request, context.Response);
            });

            return endpoints;
        }
    }


}
