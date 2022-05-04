﻿using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Core;

namespace Sparc.Platforms.Maui;

public static class SelfHostedServiceCollectionExtensions
{
    public static IServiceCollection AddSelfHostedApi<T>(this IServiceCollection services, string apiName, string baseUrl, string clientId, string callbackScheme) where T : class
    {
        // Default scope is ApiName minus spaces (scopes are delimited by spaces)
        var scope = apiName.Replace(" ", ".");

        var options = new OidcClientOptions
        {
            Authority = baseUrl.TrimEnd('/'),
            ClientId = clientId,
            RedirectUri = $"{callbackScheme}://",
            Scope = $"openid profile {scope} offline_access",
            Browser = new WebAuthenticatorBrowser($"{callbackScheme}://")
        };

        services.AddSingleton(_ => new SelfHostedAuthenticator().WithOptions(options));
        services.AddSingleton<AuthenticationStateProvider>(s => s.GetRequiredService<SelfHostedAuthenticator>());
        services.AddSingleton<ISparcAuthenticator>(s => s.GetRequiredService<SelfHostedAuthenticator>());
         
        services.AddSingleton<SelfHostedAuthorizationMessageHandler>();
        services.AddHttpClient("api")
            .AddHttpMessageHandler<SelfHostedAuthorizationMessageHandler>();

        services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), baseUrl, x.GetService<IHttpClientFactory>().CreateClient("api")));

        return services;
    }
}