using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom;

namespace Sparc.Blossom.Authentication;

public static class AzureADB2CServiceCollectionExtensions
{
    public static Task<IServiceCollection> AddB2CApi<T>(this IServiceCollection services, string baseUrl, AzureADB2CSettings b2CSettings) where T : class
    {
        services.AddAuthorizationCore();
        services.AddScoped(_ => b2CSettings);
        services.AddSingleton<AzureADB2CAuthenticator>();
        services.AddSingleton<AuthenticationStateProvider>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddSingleton<IAuthenticator>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddScoped<BlossomAuthorizationMessageHandler>();

        services.AddHttpClient("api")
            .AddHttpMessageHandler<BlossomAuthorizationMessageHandler>();

        services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), baseUrl, x.GetService<IHttpClientFactory>().CreateClient("api")));

        return Task.FromResult(services);
    }
}
