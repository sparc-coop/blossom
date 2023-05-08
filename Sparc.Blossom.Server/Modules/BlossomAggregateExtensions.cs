using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public static class BlossomAggregateExtensions
{
    public static async Task<T> InvokeAsync<T>(this Delegate method, params object?[]? args) where T : class
    {
        var result = method.DynamicInvoke(args);
        if (result is Task<T> task) return await task;
        return result as T ?? throw new InvalidOperationException("Invalid return type");
    }

    public static IServiceCollection RegisterAggregates(this IServiceCollection services)
    {
        var modules = DiscoverAggregates();
        foreach (var module in modules)
        {
            //var entity = module.GetGenericArguments().First();
            //services.AddScoped(typeof(IRepository<>).MakeGenericType(entity));
            services.AddSingleton(module);
        }

        return services;
    }

    private static IEnumerable<Type> DiscoverAggregates()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var aggregates = assemblies.Distinct().SelectMany(x => x.GetTypes())
            .Where(x => typeof(IBlossomAggregate).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

        return aggregates;
    }

    public static void MapAggregates(this WebApplication app)
    {
        var aggregates = DiscoverAggregates();
        foreach (var aggregate in aggregates)
        {
            var instance = app.Services.GetRequiredService(aggregate) as IBlossomAggregate;
            instance?.MapEndpoints(app);
        }
    }
}