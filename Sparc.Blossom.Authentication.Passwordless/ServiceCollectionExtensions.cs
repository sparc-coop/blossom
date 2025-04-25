using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Passwordless;
using Microsoft.Extensions.Configuration;
namespace Sparc.Blossom.Authentication.Passwordless;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomPasswordlessAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        var passwordlessSettings = builder.Configuration.GetRequiredSection("Passwordless");
        builder.Services.Configure<PasswordlessOptions>(passwordlessSettings);
        builder.Services.AddPasswordlessSdk(passwordlessSettings.Bind);

        return builder;
    }

    public static WebApplicationBuilder AddBlossomPasswordlessAuthenticationClient<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    { 
        builder.Services.AddScoped<AuthenticationStateProvider, BlossomAuthenticationStateProvider<TUser>>()
            .AddScoped<BlossomPasswordlessAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, BlossomPasswordlessAuthenticator<TUser>>();

        return builder;
    }
}
