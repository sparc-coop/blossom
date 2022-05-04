using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Core;

namespace Sparc.Platforms.Maui;

public static class AzureADB2CServiceCollectionExtensions
{
    public static Task<IServiceCollection> AddB2CApi<T>(this IServiceCollection services, string baseUrl, AzureADB2CSettings b2CSettings) where T : class
    {
        services.AddAuthorizationCore();
        services.AddScoped(_ => b2CSettings);
        services.AddSingleton<AzureADB2CAuthenticator>();
        services.AddSingleton<AuthenticationStateProvider>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddSingleton<ISparcAuthenticator>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddScoped<SparcAuthorizationMessageHandler>();

        services.AddHttpClient("api")
            .AddHttpMessageHandler<SparcAuthorizationMessageHandler>();

        services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), baseUrl, x.GetService<IHttpClientFactory>().CreateClient("api")));

        return Task.FromResult(services);
    }
}
