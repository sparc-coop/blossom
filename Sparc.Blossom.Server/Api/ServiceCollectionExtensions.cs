using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static async Task<T> InvokeAsync<T>(this Delegate method, params object?[]? args) where T : class
    {
        var result = method.DynamicInvoke(args);
        if (result is Task<T> task) return await task;
        return result as T ?? throw new InvalidOperationException("Invalid return type");
    }

    public static IServiceCollection AddBlossomContexts(this IServiceCollection services, Assembly assembly)
    {
        var modules = DiscoverContexts(assembly);
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

    private static IEnumerable<Type> DiscoverContexts(Assembly assembly)
    {
        var aggregates = assembly.GetTypes()
            .Where(x => typeof(IBlossomApiContext).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

        return aggregates;
    }

    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var contexts = DiscoverContexts(assembly);
        foreach (var context in contexts)
        {
            var instance = app.Services.GetRequiredService(context) as IBlossomApiContext;
            instance?.MapEndpoints(app);
        }
    }
}