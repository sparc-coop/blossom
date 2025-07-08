using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Sparc.Blossom.Payment.Stripe;

public class StripeWebhookHandler(string webhookSecret, ILogger<StripeWebhookHandler> logger)
{
    public async Task HandleAsync(HttpRequest request, HttpResponse response)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                request.Headers["Stripe-Signature"],
                webhookSecret
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe signature verification failed.");
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsync("Invalid signature");
            return;
        }

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                // ... do something (log, update DB, etc.)
                logger.LogInformation("Payment Intent {Id} succeeded.", paymentIntent?.Id);
                break;

            case "payment_intent.payment_failed":
                var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                // ... handle failed payment
                logger.LogWarning("Payment Intent {Id} failed.", failedIntent?.Id);
                break;
        }

        response.StatusCode = StatusCodes.Status200OK;
        await response.WriteAsync("Webhook handled");
    }
}

