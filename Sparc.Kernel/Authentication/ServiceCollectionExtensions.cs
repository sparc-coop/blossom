using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Sparc.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddPasswordlessAuthentication<TUser>(this WebApplicationBuilder builder, AuthenticationBuilder auth) where TUser : SparcUser
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


}
