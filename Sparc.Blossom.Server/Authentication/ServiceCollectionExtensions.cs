using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Blossom.Server.Authentication;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, BlossomAuthenticationStateProvider<TUser>>();
        builder.Services.AddScoped<BlossomDeviceAuthenticator<TUser>>();
        builder.Services.AddScoped(typeof(IBlossomAuthenticator), typeof(BlossomDeviceAuthenticator<TUser>));

        //builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
        //    .AddIdentityCookies();

        return builder;
    }
}
