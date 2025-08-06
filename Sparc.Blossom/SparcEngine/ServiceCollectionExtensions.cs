using Microsoft.Extensions.DependencyInjection;
using Refit;
using Sparc.Engine.Aura;
using Sparc.Engine.Billing;
using Sparc.Engine.Tovik;

namespace Sparc.Engine;

public static class ServiceCollectionExtensions
{
    public static void AddSparcEngine(this IServiceCollection services, string? url = null)
    {
        url ??= "https://engine.sparc.coop";
        var uri = new Uri(url);

        services.AddHttpContextAccessor();
        services.AddTransient<SparcAuraTokenHandler>();

        services.AddRefitClient<ISparcAura>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddRefitClient<ISparcBilling>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddRefitClient<ITovik>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraTokenHandler>()
            .AddStandardResilienceHandler(x =>
            {
                x.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);
                x.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(240);
                x.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
            });

        services.AddSparcAura();

        services.AddScoped<SparcEvents>();
    }
}
