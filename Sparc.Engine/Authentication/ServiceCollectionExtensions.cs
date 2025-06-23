using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Passwordless;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Platforms.Server;
using System.Security.Claims;

namespace Sparc.Engine;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcEngineAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<TUser>>()
            .AddScoped<SparcEngineAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, SparcEngineAuthenticator<TUser>>();

        builder.Services.AddTransient(s =>
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity()));

        builder.Services.AddPasswordlessSdk(x =>
        {
            x.ApiKey = SparcEngineAuthenticator<TUser>.PublicKey;
            x.ApiSecret = builder.Configuration.GetConnectionString("Passwordless") ?? throw new InvalidOperationException("Passwordless API Secret is not configured.");
        });

        //builder.Services.AddScoped<BlossomPasswordlessAuthenticator<TUser>>()
        //    .AddScoped<IBlossomAuthenticator, BlossomPasswordlessAuthenticator<TUser>>();

        return builder;
    }

    public static WebApplication UseSparcEngineAuthentication<TUser>(this WebApplication app)
        where TUser : BlossomUser, new()
    { 
        app.UseCookiePolicy(new() { 
            MinimumSameSitePolicy = SameSiteMode.None,
            HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
            Secure = CookieSecurePolicy.Always
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<SparcEngineAuthenticatorMiddleware>();

        using var scope = app.Services.CreateScope();
        var passwordlessAuthenticator =
            scope.ServiceProvider.GetRequiredService<SparcEngineAuthenticator<TUser>>();

        passwordlessAuthenticator.Map(app);

        //var auth = app.MapGroup("/auth");
        //auth.MapPost("login", async (BlossomPasswordlessAuthenticator<TUser> auth, ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null) => await auth.Login(principal, context, emailOrToken));
        //auth.MapPost("logout", async (BlossomPasswordlessAuthenticator<TUser> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.Logout(principal, emailOrToken));
        //auth.MapGet("userinfo", async (BlossomPasswordlessAuthenticator<TUser> auth, ClaimsPrincipal principal) => await auth.GetAsync(principal));

        return app;
    }

}
