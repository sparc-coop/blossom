using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static async Task<T> InvokeAsync<T>(this Delegate method, params object?[]? args) where T : class
    {
        var result = method.DynamicInvoke(args);
        if (result is Task<T> task) return await task;
        return result as T ?? throw new InvalidOperationException("Invalid return type");
    }

    public static IServiceCollection AddBlossomContexts<T>(this IServiceCollection services)
    {
        var modules = DiscoverContexts<T>();
        foreach (var module in modules)
        {
            //var entity = module.GetGenericArguments().First();
            //services.AddScoped(typeof(IRepository<>).MakeGenericType(entity));
            services.AddSingleton(module);
        }

        services.AddGrpc().AddJsonTranscoding();
        services.AddGrpcSwagger();

        return services;
    }

    private static IEnumerable<Type> DiscoverContexts<T>()
    {
        var aggregates = typeof(T).Assembly.GetTypes()
            .Where(x => typeof(IBlossomApiContext).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

        return aggregates;
    }

    public static void MapBlossomContexts<T>(this WebApplication app)
    {
        var aggregates = DiscoverContexts<T>();
        foreach (var aggregate in aggregates)
        {
            var instance = app.Services.GetRequiredService(aggregate) as IBlossomApiContext;
            instance?.MapEndpoints(app);
        }
    }
}