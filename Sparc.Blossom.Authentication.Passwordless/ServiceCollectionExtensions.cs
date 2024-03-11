using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Authentication.Passwordless;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPasswordless<TUser>(this IServiceCollection services, IConfiguration config) where TUser : BlossomUser, new()
    {
        services.AddPasswordlessSdk(options =>
        {
            options.ApiKey = config["Passwordless:ApiKey"];
            options.ApiSecret = config["Passwordless:ApiSecret"];
        });
        services.AddScoped<BlossomPasswordlessAuthenticator<TUser>>();
        services.AddScoped<BlossomAuthenticator, BlossomPasswordlessAuthenticator<TUser>>();
        services.AddScoped(typeof(BlossomAuthenticator<TUser>), typeof(BlossomPasswordlessAuthenticator<TUser>));
        return services;
    }
}
