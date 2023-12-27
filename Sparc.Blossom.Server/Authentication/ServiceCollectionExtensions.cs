using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
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
        builder.Services.AddScoped<BlossomAuthenticator<TUser>>();
        builder.Services.AddScoped(typeof(BlossomAuthenticator), typeof(BlossomAuthenticator<TUser>));

        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
            .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        builder.Services.AddIdentityCore<TUser>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddIdentityApiEndpoints<TUser>();

        return builder;
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
            var user = await authenticator.LoginAsync(userId, token, "Link");

            if (user == null)
                return Results.Unauthorized();

            await context.SignInAsync(IdentityConstants.ApplicationScheme, user.CreatePrincipal());
            return Results.Redirect(returnUrl);
        });
    }
}
