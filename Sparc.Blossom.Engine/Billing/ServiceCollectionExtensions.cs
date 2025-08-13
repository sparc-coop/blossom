using Sparc.Blossom.Billing.Stripe;

namespace Sparc.Blossom.Billing;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcBilling(
        this WebApplicationBuilder builder
    )
    {
        builder.Services.AddScoped<StripePaymentService>();
        builder.Services.AddScoped<ExchangeRates>()
            .AddScoped<BlossomAggregateOptions<SparcOrder>>()
            .AddScoped<Orders>();

        return builder;
    }

    public static WebApplication UseSparcBilling(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        var billingSvc = scope
            .ServiceProvider
            .GetRequiredService<Orders>();

        billingSvc.Map(app);
        return app;
    }
}


