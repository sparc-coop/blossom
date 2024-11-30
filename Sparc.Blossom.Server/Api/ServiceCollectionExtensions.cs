using Ardalis.Specification;
using MediatR;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Reflection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static IEnumerable<Type> GetDtos(this Assembly assembly)
       => assembly.GetDerivedTypes(typeof(BlossomApiContext<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

    public static void RegisterBlossomContexts(this WebApplicationBuilder builder, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly()!;

        builder.Services.AddScoped(typeof(BlossomApiContext<>));

        var apis = assembly.GetDerivedTypes(typeof(BlossomApiContext<>));
        foreach (var api in apis)
            builder.Services.AddScoped(api);

        var aggregates = assembly.GetAggregates();
        builder.Services.AddScoped(typeof(BlossomAggregateOptions<>));
        builder.Services.AddScoped(typeof(BlossomAggregate<>));
        foreach (var aggregate in aggregates)
            builder.Services.AddScoped(typeof(BlossomAggregate<>).MakeGenericType(aggregate), aggregate);

        var entities = assembly.GetEntities();
        foreach (var entity in entities)
        {
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(entity),
                typeof(BlossomAggregate<>).MakeGenericType(entity));

            builder.Services.AddTransient(
                typeof(INotificationHandler<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)),
                typeof(BlossomEventDefaultHandler<>).MakeGenericType(entity));
        }

        var dtos = assembly.GetDtos()
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null);

        foreach (var dto in dtos)
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(dto.Key),
                typeof(BlossomDirectRunner<,>).MakeGenericType(dto.Key, dto.Value!));

        foreach (var api in assembly.GetTypes().Where(t => typeof(IBlossomApi).IsAssignableFrom(t)))
            builder.Services.AddScoped(api);
    }

    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var entities = assembly.GetEntities();
        using var scope = app.Services.CreateScope();
        foreach (var entity in entities)
        {
            var instance = scope.ServiceProvider.GetRequiredService(typeof(IRunner<>).MakeGenericType(entity)) as IBlossomEndpointMapper;
            instance?.MapEndpoints(app);
        }
    }
}