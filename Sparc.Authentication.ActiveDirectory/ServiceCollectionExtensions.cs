using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Sparc.Authentication.ActiveDirectory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActiveDirectoryAuthentication(this IServiceCollection services, IConfigurationSection configuration)
        {
            services.AddAuthentication()
                .AddMicrosoftIdentityWebApi(configuration);

            return services;
        }


    }
}
