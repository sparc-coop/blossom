using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Realtime;

public static class BlossomRealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomRealtime(this IServiceCollection services, AppDomain domain)
    {
        var handlers = domain.GetDerivedTypes(typeof(BlossomOn<>));
        foreach (var handler in handlers)
        {
            var eventType = handler.BaseType!.GetGenericArguments().First();
            services.AddScoped(typeof(BlossomOn<>).MakeGenericType(eventType), handler);
        }

        services.AddSingleton<BlossomEvents>()
            .AddSingleton<IBlossomEvents>(s => s.GetRequiredService<BlossomEvents>());

        return services;
    }
}
