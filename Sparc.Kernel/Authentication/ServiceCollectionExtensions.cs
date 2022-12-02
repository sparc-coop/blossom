using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Sparc.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Sparc.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddPasswordlessAuthentication<TUser>(this WebApplicationBuilder builder, AuthenticationBuilder auth) where TUser : SparcUser, new()
    {
        auth.AddJwtBearer("Passwordless", o =>
        {
            var Key = Encoding.UTF8.GetBytes(builder.Configuration["Passwordless:Key"]!);
            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Passwordless:Issuer"],
                ValidAudience = builder.Configuration["Passwordless:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Key)
            };
        });

        builder.Services.AddScoped<IUserStore<TUser>, SparcUserRepository<TUser>>()
            .AddScoped<IRoleStore<SparcRole>, SparcRoleStore>();
        
        builder.Services.AddIdentity<TUser, SparcRole>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "Passwordless")
                    .Build();
            });

        return auth;
    }

    public static AuthenticationBuilder AddSparcAuthentication<TUser>(this WebApplicationBuilder builder) where TUser : SparcUser, new()
    {
        var auth = builder.Services.AddAuthentication().AddJwtBearer();

        builder.Services.AddScoped<IUserStore<TUser>, SparcUserRepository<TUser>>()
            .AddScoped<IRoleStore<SparcRole>, SparcRoleStore>();

        builder.Services.AddIdentity<TUser, SparcRole>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthorization();

        return auth;
    }

    public static void UsePasswordlessAuthentication<TUser>(this WebApplication app) where TUser : SparcUser
    {
        app.MapGet("/PasswordlessLogin", async (string userId, string token, string returnUrl, UserManager<TUser> users, HttpContext context, IOptionsSnapshot<JwtBearerOptions> config) =>
        {
            var user = await users.FindByIdAsync(userId);
            if (user == null)
                throw new NotAuthorizedException($"Can't find user {userId}");

            var isValid = await users.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

            if (isValid)
            {
                await context.SignInAsync(IdentityConstants.ApplicationScheme, user.CreatePrincipal());

                var returnUri = new Uri(returnUrl);
                var callbackUrl = $"{returnUri.Scheme}://{returnUri.Authority}/authentication/login-callback";
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "returnUrl", returnUrl);
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "passwordless", user.CreateToken(config.Value));
                return Results.Redirect(callbackUrl);
            }
            return Results.Unauthorized();

        });
    }
}
