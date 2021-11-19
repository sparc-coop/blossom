using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sparc.Authentication.Blazor
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddB2CApi<T>(this WebAssemblyHostBuilder builder, string apiScope, string baseUrl = null) where T : class
        {
            return builder.AddActiveDirectoryApi<T>(apiScope, baseUrl, "AzureAdB2C");
        }

        public static IServiceCollection AddActiveDirectoryApi<T>(this WebAssemblyHostBuilder builder, string apiScope, string baseUrl = null) where T : class
        {
            return builder.AddActiveDirectoryApi<T>(apiScope, baseUrl, "AzureAd");
        }

        public static IServiceCollection AddActiveDirectoryApi<T>(this WebAssemblyHostBuilder builder, string apiScope, string baseUrl, string configurationSectionName) where T : class
        {
            var client = builder.Services.AddHttpClient("api");
            var publicClient = builder.Services.AddHttpClient("publicApi");

            builder.Services.AddScoped(sp => new SparcAuthorizationMessageHandler(sp.GetService<IAccessTokenProvider>(), sp.GetService<NavigationManager>(), baseUrl));

            if (string.IsNullOrWhiteSpace(baseUrl))
                client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
            else
                client.AddHttpMessageHandler<SparcAuthorizationMessageHandler>();

            builder.Services.AddScoped(x =>
                (T)Activator.CreateInstance(typeof(T),
                baseUrl ?? builder.HostEnvironment.BaseAddress,
                x.GetService<IHttpClientFactory>().CreateClient("api")));

            builder.Services.AddScoped<Public<T>>(x => () =>
                (T)Activator.CreateInstance(typeof(T),
                baseUrl ?? builder.HostEnvironment.BaseAddress,
                x.GetService<IHttpClientFactory>().CreateClient("publicApi")));

            builder.Services.AddScoped<ApiResolver<T>>(x => key =>
                (T)Activator.CreateInstance(typeof(T),
                baseUrl ?? builder.HostEnvironment.BaseAddress,
                x.GetService<IHttpClientFactory>().CreateClient(key)));

            if (configurationSectionName.Contains("B2C"))
            {
                builder.Services.AddMsalAuthentication(options =>
                {
                    builder.Configuration.Bind(configurationSectionName, options.ProviderOptions.Authentication);
                    options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
                    options.UserOptions.NameClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
                });
            }

            return builder.Services;
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

            services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), baseUrl, x.GetService<IHttpClientFactory>().CreateClient("api")));

            return services;
        }
    }

    public class SparcAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public SparcAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, string baseUrl) : base(provider, navigation)
        {
            ConfigureHandler(authorizedUrls: new[] { baseUrl });
        }
    }
}
