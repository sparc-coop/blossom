using IdentityServer4.Models;
using Sparc.Core;

namespace Sparc.Authentication.SelfHosted;

public static class ServiceCollectionExtensions
{
    public static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, string clientId, string clientUrl) where T : SparcAuthenticator
    {
        var clients = new[] { services.CreateClient(clientId, clientUrl) };
        return services.AddSelfHostedAuthentication<T>(clients);
    }

    public static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, params (string, string)[] clients) where T : SparcAuthenticator
    {
        return services.AddSelfHostedAuthentication<T>(clients.Select(x => services.CreateClient(x.Item1, x.Item2)).ToArray());
    }

    private static IIdentityServerBuilder AddSelfHostedAuthentication<T>(this IServiceCollection services, IEnumerable<Client> clients) where T : SparcAuthenticator
    {
        var apiName = typeof(T).Namespace!;

        services.AddRazorPages(); // for Login UI
        services.AddHttpContextAccessor();

        ApiScopes = new List<ApiScope>() { new ApiScope(apiName.Replace(" ", "."), apiName) };
        foreach (var client in clients)
            client.AllowedScopes = ApiScopes.Select(x => x.Name).Union(IdentityScopes.Select(x => x.Name)).ToList();

        services.AddAuthentication().AddJwtBearer();
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
