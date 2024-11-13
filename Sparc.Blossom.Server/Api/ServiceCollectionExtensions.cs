using Ardalis.Specification;
using MediatR;
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

        var entities = assembly.GetEntities();
        foreach (var entity in entities)
        {
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(entity),
                typeof(BlossomServerRunner<>).MakeGenericType(entity));

            builder.Services.AddTransient(
                typeof(INotificationHandler<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)),
                typeof(BlossomEventDefaultHandler<>).MakeGenericType(entity));
        }

        var openEndpoints = assembly.GetTypes().Where(t => typeof(IBlossomEndpointMapper).IsAssignableFrom(t));
        foreach (var endpoint in openEndpoints)
        {
            builder.Services.AddScoped(typeof(IBlossomEndpointMapper), endpoint);
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
        app.MapOpenApi();

        using var scope = app.Services.CreateScope();
        foreach (var endpointMapper in scope.ServiceProvider.GetServices<IBlossomEndpointMapper>())
            endpointMapper.MapEndpoints(app);
    }
}