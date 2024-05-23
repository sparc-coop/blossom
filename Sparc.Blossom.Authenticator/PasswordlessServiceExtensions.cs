using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Passwordless.Net;

namespace Sparc.Blossom.Authenticator
{
    public static class PasswordlessServiceExtensions
    {
        public static IServiceCollection AddPasswordlessAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var passwordlessSettings = configuration.GetRequiredSection("Passwordless");
            services.Configure<PasswordlessOptions>(passwordlessSettings);
            services.AddPasswordlessSdk(options => passwordlessSettings.Bind(options));
            services.AddScoped<PasswordlessService>();

            return services;
        }
    }
}
