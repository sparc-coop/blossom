using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Blossom.Server.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, BlossomDefaultAuthenticator<TUser>>()
            .AddScoped<BlossomDefaultAuthenticator<TUser>>()
            .AddScoped(typeof(IBlossomAuthenticator), typeof(BlossomDefaultAuthenticator<TUser>));
        return builder;
    }

    public static IApplicationBuilder UseBlossomAuthentication(this IApplicationBuilder app)
    {
        app.UseCookiePolicy(new() { MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<BlossomDefaultAuthenticatorMiddleware>();

        return app;
    }
}
