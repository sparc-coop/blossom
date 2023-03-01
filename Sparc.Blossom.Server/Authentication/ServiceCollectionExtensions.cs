using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddBlossomAuthentication<TUser>(this WebApplicationBuilder builder)
        where TUser : BlossomUser, new()
    {
        var auth = builder.Services.AddAuthentication().AddCookie(opt =>
        {
            opt.Cookie.Name = "__Host-blossom";
            opt.Cookie.SameSite = SameSiteMode.Strict;
        });

        builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
            .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        builder.Services.AddIdentity<TUser, BlossomRole>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-blossom";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = new PathString("/_auth/login");
            options.LogoutPath = new PathString("/_auth/logout");
            options.SlidingExpiration = true;
            options.Cookie.MaxAge = options.ExpireTimeSpan;
        });
        
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<BlossomAuthenticator<TUser>>();
        builder.Services.AddScoped(typeof(BlossomAuthenticator), typeof(BlossomAuthenticator<TUser>));

        return auth;
    }

    public static void UseBlossomAuthentication<TUser>(this WebApplication app) where TUser : BlossomUser, new()
    {
        app.MapGet("/_auth/userinfo", async (UserManager<TUser> users, ClaimsPrincipal principal) =>
        {
            if (principal.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var user = await users.FindByIdAsync(principal.Id());
            return Results.Ok(user);
        });
        
        app.MapGet("/_auth/login-silent", 
            async (string userId, string token, string returnUrl, HttpContext context, BlossomAuthenticator<TUser> authenticator) =>
        {
            var user = await authenticator.LoginAsync(userId, token);

            if (user == null)
                return Results.Unauthorized();

            await context.SignInAsync(IdentityConstants.ApplicationScheme, user.CreatePrincipal());
            return Results.Redirect(returnUrl);
        });
    }
}
