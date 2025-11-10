using Microsoft.Extensions.DependencyInjection;
using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Engine;

public static class BrowserServiceCollectionExtensions
{
    public static void AddBlossomEngine(this IServiceCollection services, string? url = null)
    {
        url ??= "https://engine.sparc.coop";
        var uri = new Uri(url);

        services.AddTransient<SparcAuraBrowserTokenHandler>();

        services.AddRefitClient<ISparcAura>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraBrowserTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddRefitClient<ISparcBilling>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraBrowserTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddRefitClient<ITovik>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraBrowserTokenHandler>()
            .AddStandardResilienceHandler(x =>
            {
                x.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);
                x.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(240);
                x.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
            });

        services.AddRefitClient<ISparcChat>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcAuraBrowserTokenHandler>()
            .AddStandardResilienceHandler();

        services.AddSparcAura();

        services.AddScoped<SparcEvents>();
    }

    public static void AddSparcAura(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<SparcAuraBrowserAuthenticator>()
            .AddScoped<IBlossomAuthenticator, SparcAuraBrowserAuthenticator>()
            .AddScoped<PasskeyAuthenticator>();
    }
}
