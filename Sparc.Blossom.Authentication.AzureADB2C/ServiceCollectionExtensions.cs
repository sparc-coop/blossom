using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Sparc.Blossom.Authentication;

public static class ServiceCollectionExtensions
{
    public static AuthenticationBuilder AddAzureADB2CAuthentication<TUser>(this IServiceCollection services, IConfiguration configuration, string configurationSectionName = "AzureAdB2C") where TUser : IUser
    {
        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        builder.AddMicrosoftIdentityWebApi(
                configuration,
                configurationSectionName,
                JwtBearerDefaults.AuthenticationScheme,
                false);

        //GDPR
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
            options.HandleSameSiteCookieCompatibility();
        });

        // To fix User.Identity.Name
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.NameClaimType = "name";
        });

        services.AddTransient<IClaimsTransformation, AzureAdB2CClaimsTransformation<TUser>>();

        return builder;
    }
}
