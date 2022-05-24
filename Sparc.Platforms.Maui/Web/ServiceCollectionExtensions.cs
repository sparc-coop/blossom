using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Sparc.Core;
using Sparc.Blossom;

namespace Sparc.Platforms.Web;

public static class ServiceCollectionExtensions
{
    public static WebAssemblyHostBuilder Sparcify(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<Core.Device, WebDevice>();
        builder.Services.AddScoped<IConfiguration>(_ => builder.Configuration);

        return builder;
    }

    public static WebAssemblyHostBuilder AddB2CApi<T>(this WebAssemblyHostBuilder builder, string baseUrl = null) where T : class
    {
        var client = builder.Services.AddHttpClient("api");

        builder.Services.AddScoped(sp => new SparcAuthorizationMessageHandler(sp.GetService<IAccessTokenProvider>(), sp.GetService<NavigationManager>(), baseUrl));

        if (string.IsNullOrWhiteSpace(baseUrl))
            client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
        else
            client.AddHttpMessageHandler<SparcAuthorizationMessageHandler>();

        builder.Services.AddScoped(x =>
            (T)Activator.CreateInstance(typeof(T),
            baseUrl,
            x.GetService<IHttpClientFactory>().CreateClient("api")));

        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
            options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["AzureAdB2C:Scope"]);
            options.UserOptions.NameClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        });

        builder.Services.AddSingleton<RootScope>();

        return builder;
    }

    public static IServiceCollection AddSelfHostedApi<T>(this IServiceCollection services, string apiName, string baseUrl, string clientId) where T : class
    {
        // Default scope is ApiName minus spaces (scopes are delimited by spaces)
        var scope = apiName.Replace(" ", ".");

        // Add Blazor WebAssembly auth
        services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = baseUrl;
            options.ProviderOptions.ClientId = clientId;
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.DefaultScopes.Add(scope);
        });

        services.AddScoped(sp => new SparcAuthorizationMessageHandler(sp.GetService<IAccessTokenProvider>(), sp.GetService<NavigationManager>(), baseUrl));
        services.AddHttpClient("api")
            .AddHttpMessageHandler<SparcAuthorizationMessageHandler>();

        services.AddSingleton<RootScope>();

        return services;
    }
}
