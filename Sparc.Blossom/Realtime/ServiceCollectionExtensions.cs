using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sparc.Blossom.Realtime;

public static class BlossomRealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomRealtime(this IServiceCollection services, Assembly assembly)
    {
        var handlers = assembly.GetDerivedTypes(typeof(BlossomOn<>));
        foreach (var handler in handlers)
        {
            var eventType = handler.BaseType!.GetGenericArguments().First();
            services.AddScoped(typeof(BlossomOn<>).MakeGenericType(eventType), handler);
        }

        services
            .AddSingleton<BlossomEvents>()
            .AddSingleton<IBlossomEvents, BlossomEvents>();

        return services;
    }
}
