using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace Sparc.Blossom.Payment.Stripe;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStripePayments(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<StripePaymentService>();
        services.AddSingleton<ExchangeRates>();
        StripeConfiguration.ApiKey = config.GetConnectionString("Stripe");
        ExchangeRates.ApiKey = config.GetConnectionString("ExchangeRates")
            ?? throw new Exception("ExchangeRates connection string not configured");

        return services;
    }
}
