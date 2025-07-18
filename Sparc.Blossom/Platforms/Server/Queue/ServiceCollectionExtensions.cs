using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom;

public static class QueueServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomService<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton(typeof(BlossomQueue<>))
                .AddHostedService<BlossomBackgroundService<T>>()
                .AddScoped<T>();

        return services;
    }

    public static IServiceCollection AddBlossomTimedService<T>(this IServiceCollection services, TimeSpan timespan) where T : class, IBlossomService
    {
        services
            .AddScoped<T>()
            .AddHostedService(x => new BlossomTimedBackgroundService<T>(x.GetRequiredService<IServiceScopeFactory>(), timespan));
        return services;
    }
}
