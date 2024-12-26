using Ardalis.Specification;
using System.Reflection;

namespace Sparc.Blossom;

public static partial class ServiceCollectionExtensions
{
    public static IEnumerable<Type> GetDtos(this Assembly assembly)
       => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

    public static void RegisterBlossomContexts(this WebApplicationBuilder builder, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly()!;

        builder.Services.AddScoped(typeof(BlossomAggregateProxy<>));

        var apis = assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>));
        foreach (var api in apis)
            builder.Services.AddScoped(api);

        var aggregates = assembly.GetAggregates();
        builder.Services.AddScoped(typeof(BlossomAggregateOptions<>));
        builder.Services.AddScoped(typeof(BlossomAggregate<>));

        var entities = assembly.GetEntities();
        foreach (var entity in entities)
        {
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(entity),
                typeof(BlossomAggregate<>).MakeGenericType(entity));

            builder.Services.AddScoped(
                typeof(IRepository<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)),
                typeof(BlossomInMemoryRepository<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)));

        }

        foreach (var aggregate in aggregates)
        {
            var baseOfType = aggregate.BaseType!.GenericTypeArguments.First();
            builder.Services.AddScoped(typeof(BlossomAggregate<>).MakeGenericType(baseOfType), aggregate);
            builder.Services.AddScoped(typeof(IRunner<>).MakeGenericType(baseOfType), aggregate);
            builder.Services.AddScoped(aggregate);
        }

        var dtos = assembly.GetDtos()
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null);

        foreach (var dto in dtos)
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(dto.Key),
                typeof(BlossomDirectRunner<,>).MakeGenericType(dto.Key, dto.Value!));

        foreach (var api in assembly.GetTypes<IBlossomApi>())
            builder.Services.AddScoped(api);
    }

    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var endpoints = assembly.GetTypes<IBlossomEndpointMapper>();
        using var scope = app.Services.CreateScope();
        foreach (var endpoint in endpoints)
        {
            var instance = scope.ServiceProvider.GetRequiredService(endpoint) as IBlossomEndpointMapper;
            instance?.MapEndpoints(app);
        }
    }
}