using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddBlossomAuthentication<T, TUser>(this WebApplicationBuilder builder)
        where T : BlossomAuthenticator
        where TUser : BlossomUser, new()
    {
        var auth = builder.Services.AddAuthentication().AddCookie(opt =>
        {
            opt.Cookie.IsEssential = true;
            opt.Cookie.HttpOnly = true;
            opt.Cookie.SameSite = SameSiteMode.Strict;
            opt.SlidingExpiration = true;
        });

        builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
            .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        builder.Services.AddIdentity<TUser, BlossomRole>()
            .AddDefaultTokenProviders();
        
        builder.Services.AddAuthorization();

        builder.Services.AddScoped(typeof(BlossomAuthenticator), typeof(T));

        return auth;
    }

    public static void UsePasswordlessAuthentication<TUser>(this WebApplication app) where TUser : BlossomUser
    {
        app.MapGet("/auth/login-passwordless", 
            async (string userId, string token, string returnUrl, UserManager<TUser> users, HttpContext context, BlossomAuthenticator authenticator) =>
        {
            var user = await users.FindByIdAsync(userId) 
                ?? throw new NotAuthorizedException($"Can't find user {userId}");
            
            var isValid = await users.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

            if (!isValid)
                return Results.Unauthorized();

            await context.SignInAsync(IdentityConstants.ApplicationScheme, user.CreatePrincipal());
            return Results.Redirect(returnUrl);
        });
    }
}
