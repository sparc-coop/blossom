using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Authentication.Blazor;

namespace Sparc.Platforms.Web.ActiveDirectory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveDirectoryApiWithCustomAccountFactory<T>(this WebAssemblyHostBuilder builder, string apiScope, string baseUrl, string configurationSectionName) where T : class
        {
            builder.AddActiveDirectoryApi<T>(apiScope, baseUrl, configurationSectionName);
            
            builder.Services.AddMsalAuthentication<RemoteAuthenticationState, CustomUserAccount>(options =>
            {
                builder.Configuration.Bind(configurationSectionName, options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
                options.UserOptions.RoleClaim = "role";
            }).AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, CustomUserAccount, CustomAccountFactory>();

            return builder.Services;
        }

    }
}
