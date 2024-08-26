using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromDays(30));

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, BlossomDefaultAuthenticator<TUser>>()
            .AddScoped<BlossomDefaultAuthenticator<TUser>>()
            .AddScoped(typeof(IBlossomAuthenticator), typeof(BlossomDefaultAuthenticator<TUser>));
        return builder;
    }

    public static IApplicationBuilder UseBlossomAuthentication(this IApplicationBuilder app)
    {
        app.UseCookiePolicy(new() { MinimumSameSitePolicy = SameSiteMode.Strict });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<BlossomDefaultAuthenticatorMiddleware>();

        return app;
    }
}
