using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Sparc.Blossom;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddPasswordlessAuthentication<TUser>(this WebApplicationBuilder builder, AuthenticationBuilder auth) where TUser : BlossomUser, new()
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

        builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
            .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        builder.Services.AddIdentity<TUser, BlossomRole>()
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

    public static AuthenticationBuilder AddBlossomAuthentication<T>(this WebApplicationBuilder builder, string? signingKey = null) where T : BlossomAuthenticator
    {
        var auth = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddCookie()
            .AddJwtBearer(options =>
        {
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? builder.Configuration["Jwt:Key"]!));
            options.TokenValidationParameters.ValidateAudience = false;
            options.TokenValidationParameters.ValidateIssuer = false;
        }
        );

        //builder.Services.AddScoped<IUserStore<TUser>, BlossomUserRepository<TUser>>()
        //    .AddScoped<IRoleStore<BlossomRole>, BlossomRoleStore>();

        //builder.Services.AddIdentity<TUser, BlossomRole>()
        //    .AddDefaultTokenProviders();

        builder.Services.AddAuthorization();

        builder.Services.AddScoped(typeof(BlossomAuthenticator), typeof(T));

        return auth;
    }

    public static void UsePasswordlessAuthentication<TUser>(this WebApplication app) where TUser : BlossomUser
    {
        app.MapGet("/PasswordlessLogin", async (string userId, string token, string returnUrl, UserManager<TUser> users, HttpContext context, BlossomAuthenticator authenticator) =>
        {
            var user = await users.FindByIdAsync(userId);
            if (user == null)
                throw new NotAuthorizedException($"Can't find user {userId}");

            var isValid = await users.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);

            if (isValid)
            {
                await context.SignInAsync(IdentityConstants.ApplicationScheme, user.CreatePrincipal());

                var returnUri = new Uri(returnUrl);
                var callbackUrl = $"{returnUri.Scheme}://{returnUri.Authority}/_authorize";
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "returnUrl", returnUrl);
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "token", authenticator.CreateToken(user));
                return Results.Redirect(callbackUrl);
            }
            return Results.Unauthorized();

        });
    }
}
