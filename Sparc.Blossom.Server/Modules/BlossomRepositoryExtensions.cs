using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom;

public static class BlossomRepositoryExtensions
{
    public static async Task<T> InvokeAsync<T>(this Delegate method, params object?[]? args) where T : class
    {
        var result = method.DynamicInvoke(args);
        if (result is Task<T> task) return await task;
        return result as T ?? throw new InvalidOperationException("Invalid return type");
    }

    public static IServiceCollection RegisterAggregates<T>(this IServiceCollection services)
    {
        var modules = DiscoverAggregates<T>();
        foreach (var module in modules)
        {
            //var entity = module.GetGenericArguments().First();
            //services.AddScoped(typeof(IRepository<>).MakeGenericType(entity));
            services.AddSingleton(module);
        }

        return services;
    }

    private static IEnumerable<Type> DiscoverAggregates<T>()
    {
        var aggregates = typeof(T).Assembly.GetTypes()
            .Where(x => typeof(IBlossomRepository).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

        return aggregates;
    }

    public static void MapAggregates<T>(this WebApplication app)
    {
        var aggregates = DiscoverAggregates<T>();
        foreach (var aggregate in aggregates)
        {
            var instance = app.Services.GetRequiredService(aggregate) as IBlossomRepository;
            instance?.MapEndpoints(app);
        }
    }
}