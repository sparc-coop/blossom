using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Passwordless.Net;
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

        builder.Services.AddScoped<AuthenticationStateProvider, BlossomPasswordlessAuthenticator<TUser>>()
            .AddScoped<BlossomPasswordlessAuthenticator<TUser>>()
            .AddScoped(typeof(IBlossomAuthenticator), typeof(BlossomPasswordlessAuthenticator<TUser>));

        return builder;
    }
}
