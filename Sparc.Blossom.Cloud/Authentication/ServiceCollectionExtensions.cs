using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Passwordless;
using Sparc.Blossom.Platforms.Server;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomPasswordlessAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromDays(30));

        builder.Services.AddAuthorization();

        builder.Services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<TUser>>()
            .AddScoped<BlossomDefaultAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<TUser>>();

        builder.Services.AddTransient(s =>
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity()));

        var passwordlessSettings = builder.Configuration.GetRequiredSection("Passwordless");
        builder.Services.Configure<PasswordlessOptions>(passwordlessSettings);
        builder.Services.AddPasswordlessSdk(passwordlessSettings.Bind);

        builder.Services.AddScoped<BlossomPasswordlessAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, BlossomPasswordlessAuthenticator<TUser>>();

        return builder;
    }

    public static WebApplication UseBlossomPasswordlessAuthentication<TUser>(this WebApplication app)
        where TUser : BlossomUser, new()
    { 
        app.UseCookiePolicy(new() { MinimumSameSitePolicy = SameSiteMode.Strict });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<BlossomAuthenticatorMiddleware>();

        app.Services.GetRequiredService<BlossomPasswordlessAuthenticator<TUser>>().Map(app);

        return app;
    }

}
