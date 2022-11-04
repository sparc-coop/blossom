using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Authentication;
using Microsoft.AspNetCore.Authorization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Polly;

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

        builder.AddPasswordlessApi<T>();

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

    public static WebAssemblyHostBuilder AddPasswordlessApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        builder.Services.AddScoped<IAccessTokenProvider, PasswordlessAccessTokenProvider>();
        builder.Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("Passwordless", policy => policy.Requirements.Add(new PasswordlessRequirement()));
        });
        builder.Services.AddScoped<IAuthorizationHandler, PasswordlessAuthorizationHandler>();
        builder.Services.AddScoped<AuthenticationStateProvider, PasswordlessAuthenticationStateProvider<RemoteAuthenticationState, RemoteUserAccount, MsalProviderOptions>>();

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
        })
            .AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            }));

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
        else
            client.AddHttpMessageHandler<BlossomAuthorizationMessageHandler>();
    }

}
