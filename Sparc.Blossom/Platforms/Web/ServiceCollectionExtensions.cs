using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Sparc.Blossom;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Web;

public static class ServiceCollectionExtensions
{
    public static WebAssemblyHostBuilder AddBlossom<T>(this WebAssemblyHostBuilder builder, string? baseUrl = null) where T : class
    {
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<Core.Device, WebDevice>();
        builder.Services.AddScoped<IConfiguration>(_ => builder.Configuration);

        if (builder.Configuration["AzureAdB2C:Authority"] != null)
            builder.AddB2CApi<T>();

        if (builder.Configuration["Oidc:Authority"] != null)
            builder.AddOidcApi<T>();

        //if (builder.Configuration["Passwordless"] != null)
        //    builder.AddPasswordlessApi<T>(baseUrl);

        builder.AddBlossomHttpClient<T>(baseUrl);

        return builder;
    }

    public static WebAssemblyHostBuilder AddB2CApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
            options.UserOptions.NameClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

            if (builder.Configuration["AzureAdB2C:Scope"] != null)
                options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["AzureAdB2C:Scope"]!);
        });


        return builder;
    }

    public static WebAssemblyHostBuilder AddOidcApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        // Add Blazor WebAssembly auth
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("Oidc", options.ProviderOptions);
            options.ProviderOptions.ResponseType = "code";
            if (builder.Configuration["Oidc:Scope"] != null)
                options.ProviderOptions.DefaultScopes.Add(builder.Configuration["Oidc:Scope"]!.Replace(" ", "."));
        });

        return builder;
    }

    public static void AddBlossomHttpClient<T>(this WebAssemblyHostBuilder builder, string? apiBaseUrl) where T : class
    {
        builder.Services.AddScoped(sp => new BlossomAuthorizationMessageHandler(
            sp.GetRequiredService<IAccessTokenProvider>(), 
            sp.GetRequiredService<NavigationManager>(),
            apiBaseUrl));

        var client = builder.Services.AddHttpClient<T>(client =>
        {
            if (apiBaseUrl != null)
                client.BaseAddress = new Uri(apiBaseUrl);
        });

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
        else
            client.AddHttpMessageHandler<BlossomAuthorizationMessageHandler>();
    }

}
