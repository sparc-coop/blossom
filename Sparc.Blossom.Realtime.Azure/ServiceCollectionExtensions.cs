using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzurePushNotifications(this IServiceCollection services, IConfigurationSection configuration)
    {
        var azureConfig = configuration.Get<AzureConfiguration>();
        services.AddSingleton(_ => azureConfig!).AddScoped<AzureNotificationService>();

        return services;
    }
}
