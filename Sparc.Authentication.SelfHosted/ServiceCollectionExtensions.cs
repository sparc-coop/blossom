using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sparc.Core;
using System.Collections.Generic;
using System.Linq;

namespace Sparc.Authentication.SelfHosted
{
    public static class ServiceCollectionExtensions
    {
        public static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, string serverUrl, string clientId, string clientUrl) where T : SparcAuthenticator
        {
            var clients = new[] { services.CreateClient(clientId, clientUrl) };
            return services.AddSelfHostedAuthentication<T>(serverUrl, clients);
        }

        public static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, string serverUrl, params (string, string)[] clients) where T : SparcAuthenticator
        {
            return services.AddSelfHostedAuthentication<T>(serverUrl, clients.Select(x => services.CreateClient(x.Item1, x.Item2)).ToArray());
        }

        private static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, string serverUrl, IEnumerable<Client> clients) where T : SparcAuthenticator
        {
            var apiName = typeof(T).Namespace!;

            services.AddRazorPages(); // for Login UI
            services.AddHttpContextAccessor();

            ApiScopes = new List<ApiScope>() { new ApiScope(apiName.Replace(" ", "."), apiName) };
            foreach (var client in clients)
                client.AllowedScopes = ApiScopes.Select(x => x.Name).Union(IdentityScopes.Select(x => x.Name)).ToList();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = serverUrl;
                    options.TokenValidationParameters = new TokenValidationParameters { ValidateAudience = false };
                });

            services.AddAuthorization();

            services.AddCors(options =>
            {
                var clientUrls = clients.SelectMany(x => x.AllowedCorsOrigins).ToArray();
                options.AddDefaultPolicy(builder => builder.WithOrigins(clientUrls).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
            });

            services.AddScoped(typeof(SparcAuthenticator), typeof(T));
            services.AddScoped(typeof(ITokenProvider), typeof(DefaultTokenProvider));

            var builder = services.AddIdentityServer()
                .AddInMemoryIdentityResources(IdentityScopes)
                .AddDeveloperSigningCredential()
                .AddInMemoryApiScopes(ApiScopes)
                .AddInMemoryClients(clients)
                .AddProfileService<SparcProfileService>();
            return builder;
        }

        public static Client CreateClient(this IServiceCollection services, string clientId, string clientUrl)
        {
            var corsUrl = clientUrl.StartsWith("http")
                ? clientUrl
                : $"https://{clientUrl.Replace("://", "")}";

            return new Client
            {
                ClientId = clientId,
                RequireClientSecret = false,
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                AllowedCorsOrigins = { corsUrl },
                RedirectUris = { $"{clientUrl}/authentication/login-callback", clientUrl },
                PostLogoutRedirectUris = { clientUrl },
                AllowOfflineAccess = true,
                AllowedScopes = ApiScopes.Select(x => x.Name).Union(IdentityScopes.Select(x => x.Name)).ToList()
            };
        }

        public static List<ApiScope> ApiScopes { get; set; } = new List<ApiScope>();
        public static List<Client> Clients { get; set; } = new List<Client>();
        public static List<IdentityResource> IdentityScopes = new()
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
        };
    }
}
