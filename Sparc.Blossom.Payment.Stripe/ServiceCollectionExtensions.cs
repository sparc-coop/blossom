using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sparc.Blossom.Payment.Stripe
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStripePayments(
            this IServiceCollection services,
            Action<StripeClientOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton<StripePaymentService>();
            return services;
        }
    }
}
