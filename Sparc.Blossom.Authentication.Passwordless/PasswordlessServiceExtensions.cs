using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Passwordless;

namespace Sparc.Blossom.Authentication;

public static class PasswordlessServiceExtensions
{
    public static IServiceCollection AddPasswordlessAuthentication<T>(this IServiceCollection services, IConfiguration configuration)
        where T : BlossomUser, new()
    {
        var passwordlessSettings = configuration.GetRequiredSection("Passwordless");
        services.Configure<PasswordlessOptions>(passwordlessSettings);
        services.AddPasswordlessSdk(passwordlessSettings.Bind);
        services.AddScoped<PasswordlessAuthenticator<T>>();
        services.AddScoped<IBlossomAuthenticator, PasswordlessAuthenticator<T>>();

        return services;
    }
}
