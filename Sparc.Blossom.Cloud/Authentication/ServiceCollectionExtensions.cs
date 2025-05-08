using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Passwordless;
using Sparc.Blossom.Platforms.Server;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomCloudAuthentication<TUser>(this WebApplicationBuilder builder)
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

    public static WebApplication UseBlossomCloudAuthentication<TUser>(this WebApplication app)
        where TUser : BlossomUser, new()
    { 
        app.UseCookiePolicy(new() { 
            MinimumSameSitePolicy = SameSiteMode.Strict,
            HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
            Secure = CookieSecurePolicy.Always
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<BlossomAuthenticatorMiddleware>();

        var auth = app.MapGroup("/auth");
        auth.MapPost("login", async (BlossomPasswordlessAuthenticator<TUser> auth, ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null) => await auth.Login(principal, context, emailOrToken));
        auth.MapGet("userinfo", async (BlossomPasswordlessAuthenticator<TUser> auth, ClaimsPrincipal principal) => await auth.GetAsync(principal));

        return app;
    }

}
