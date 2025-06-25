using Sparc.Blossom.Payment.Stripe;

namespace Sparc.Blossom.Cloud.Billing;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomCloudBilling(
        this WebApplicationBuilder builder
    )
    {
        var ratesSection = builder.Configuration.GetSection("ExchangeRates");
        var stripeSection = builder.Configuration.GetSection("Stripe");

        builder.Services.AddExchangeRates(opts =>
        {
            ratesSection.Bind(opts);
        });

        builder.Services.AddStripePayments(opts =>
        {
            stripeSection.Bind(opts);
        });


        builder.Services.AddTransient<BlossomBillingService>();

        return builder;
    }

    public static WebApplication UseBlossomCloudBilling(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        var billingSvc = scope
            .ServiceProvider
            .GetRequiredService<BlossomBillingService>();

        billingSvc.Map(app);
        return app;
    }
}



