using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Blossom.Server.Authentication;
using Microsoft.AspNetCore.Routing;

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

        //builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
        //    .AddIdentityCookies();

        builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
            .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        builder.Services.AddIdentityCore<TUser>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddIdentityApiEndpoints<TUser>();

        return builder;
    }

    public record BlossomRegistrationRequest(string Email);
    public record BlossomRegistrationResponse(string Token);
    public record BlossomLoginRequest(string Token);
    public static void MapBlossomAuthentication<TUser>(this WebApplication app) where TUser : BlossomUser, new()
    {
        app.MapGet("/_auth/userinfo", async (UserManager<TUser> users, ClaimsPrincipal principal) =>
        {
            if (principal.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var user = await users.FindByIdAsync(principal.Id());
            return Results.Ok(user);
        });

        app.MapPost("/_auth/register", async (BlossomAuthenticator<TUser> authenticator, BlossomRegistrationRequest request) =>
        {
            var user = await authenticator.RegisterAsync(request.Email);
            if (user?.Identity.SecurityStamp == null)
                return Results.BadRequest("Failed to register user.");

            return Results.Ok(new BlossomRegistrationResponse(user!.Identity.SecurityStamp));
        });

        app.MapPost("/_auth/login", async (BlossomAuthenticator<TUser> authenticator, BlossomLoginRequest request) =>
        {
            var user = await authenticator.LoginAsync(request.Token);
            return Results.Ok(user);
        });
    }
}
