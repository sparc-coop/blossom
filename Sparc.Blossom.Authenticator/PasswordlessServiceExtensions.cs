using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Passwordless.Net;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Authenticator
{
    public static class PasswordlessServiceExtensions
    {
        public static IServiceCollection AddPasswordlessAuthentication<T>(this IServiceCollection services, IConfiguration configuration)
            where T : BlossomUser, new()
        {
            var passwordlessSettings = configuration.GetRequiredSection("Passwordless");
            services.Configure<PasswordlessOptions>(passwordlessSettings);
            services.AddPasswordlessSdk(passwordlessSettings.Bind);
            services.AddScoped<PasswordlessService<T>>();
            services.AddScoped<IPasswordlessService, PasswordlessService<T>>();

            return services;
        }
    }
}
