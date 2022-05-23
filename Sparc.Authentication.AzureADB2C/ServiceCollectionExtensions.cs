using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace Sparc.Authentication.AzureADB2C;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureADB2CAuthentication(this IServiceCollection services, IConfiguration configuration, string configurationSectionName = "AzureAdB2C")
    {
        services.AddMicrosoftIdentityWebApiAuthentication(configuration, configurationSectionName);

        //GDPR
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
            options.HandleSameSiteCookieCompatibility();
        });

        services.AddControllersWithViews().AddMicrosoftIdentityUI();

        // To fix User.Identity.Name
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.NameClaimType = "name";
        });

        return services;
    }
}
