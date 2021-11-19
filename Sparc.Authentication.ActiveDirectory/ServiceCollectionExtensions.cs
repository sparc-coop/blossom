using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System;

namespace Sparc.Authentication.ActiveDirectory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveDirectoryAuthentication(this IServiceCollection services, IConfigurationSection configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration);

            return services;
        }


    }
}
