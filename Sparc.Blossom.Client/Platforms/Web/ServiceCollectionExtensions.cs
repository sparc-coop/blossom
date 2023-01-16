using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Polly;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossom<T>(this IServiceCollection services, IConfiguration configuration, string? baseUrl = null) where T : class
    {
        services.AddBlazoredLocalStorage();
        services.AddScoped<IDevice, WebDevice>();
        services.AddScoped(_ => configuration);

        var hasAuth = configuration["AzureAdB2C:Authority"] != null
            || configuration["Oidc:Authority"] != null
            || configuration["Blossom:Authority"] != null;
        
        if (configuration["AzureAdB2C:Authority"] != null)
            services.AddB2CApi<T>(configuration);
        if (configuration["Oidc:Authority"] != null)
            services.AddOidcApi<T>(configuration);
        if (configuration["Blossom:Authority"] != null)
            services.AddBlossomApi<T>();

        if (!hasAuth)
        {
            services.AddAuthorizationCore();
            services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
        }

        services.AddBlossomHttpClient<T>(baseUrl, hasAuth);
        
        return services;
    }

    public static IServiceCollection AddB2CApi<T>(this IServiceCollection services, IConfiguration configuration) where T : class
    {
        services.AddMsalAuthentication(options =>
        {
            configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
            options.UserOptions.NameClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

            if (configuration["AzureAdB2C:Scope"] != null)
                options.ProviderOptions.DefaultAccessTokenScopes.Add(configuration["AzureAdB2C:Scope"]!);
        });

        return services;
    }

    public static IServiceCollection AddOidcApi<T>(this IServiceCollection services, IConfiguration configuration) where T : class
    {
        // Add Blazor WebAssembly auth
        services.AddOidcAuthentication(options =>
        {
            configuration.Bind("Oidc", options.ProviderOptions);
            options.ProviderOptions.ResponseType = "code";
            if (configuration["Oidc:Scope"] != null)
                options.ProviderOptions.DefaultScopes.Add(configuration["Oidc:Scope"]!.Replace(" ", "."));
        });

        return services;
    }

    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services) where T : class
    {
        services.AddScoped<IAccessTokenProvider, BlossomAuthenticationStateProvider>();
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("Blossom", policy => policy.Requirements.Add(new BlossomAccessTokenRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, BlossomAccessTokenAuthorizationHandler>();
        services.AddScoped<AuthenticationStateProvider, BlossomAuthenticationStateProvider>();
        services.AddScoped<BlossomAuthenticationStateProvider>();

        return services;
    }

    public static void AddBlossomHttpClient<T>(this IServiceCollection services, string? apiBaseUrl, bool configureAuthentication = true) where T : class
    {
        services.AddScoped(sp => new BlossomAuthorizationMessageHandler(sp.GetRequiredService<IAccessTokenProvider>(), apiBaseUrl));

        var client = services.AddHttpClient<T>(client =>
        {
            if (apiBaseUrl != null)
                client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestVersion = new Version(2, 0);
        })
            .AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            }));

        if (!configureAuthentication)
            return;

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
        else
            client.AddHttpMessageHandler<BlossomAuthorizationMessageHandler>();
    }

    public static WebAssemblyHostBuilder AddBlossom<T>(this WebAssemblyHostBuilder builder, string? baseUrl = null) where T : class
    {
        builder.Services.AddBlossom<T>(builder.Configuration, baseUrl);
        return builder;
    }

    public static WebAssemblyHostBuilder AddB2CApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        builder.Services.AddB2CApi<T>(builder.Configuration);
        return builder;
    }

    public static WebAssemblyHostBuilder AddOidcApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        builder.Services.AddOidcApi<T>(builder.Configuration);
        return builder;
    }

    public static WebAssemblyHostBuilder AddBlossomApi<T>(this WebAssemblyHostBuilder builder) where T : class
    {
        builder.Services.AddBlossomApi<T>();
        return builder;
    }

    public static void AddBlossomHttpClient<T>(this WebAssemblyHostBuilder builder, string? apiBaseUrl) where T : class
    {
        builder.Services.AddBlossomHttpClient<T>(apiBaseUrl);
    }

}
