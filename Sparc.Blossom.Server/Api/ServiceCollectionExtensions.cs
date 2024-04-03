using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Data;
using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static IEnumerable<Type> GetEntities(this Assembly assembly)
        => assembly.GetTypes()
            .Where(x => typeof(Entity<>).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

    public static IEnumerable<Type> GetDtos(this Assembly assembly)
        => assembly.GetTypes()
            .Where(x => x.BaseType?.IsGenericType == true && x.GetGenericTypeDefinition() == typeof(BlossomApiContext<>))
            .Select(x => x.GetGenericArguments().First())
            .Distinct();

    public static void RegisterBlossomContexts(this WebApplicationBuilder builder, Assembly assembly)
    {
        var entities = assembly.GetEntities();

        foreach (var entity in entities)
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(entity),
                typeof(BlossomServerRunner<>).MakeGenericType(entity));

        var dtos = assembly.GetDtos()
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null);

        foreach (var dto in dtos)
            builder.Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(dto.Key),
                typeof(BlossomDirectRunner<,>).MakeGenericType(dto.Key, dto.Value!));
    }

    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var entities = assembly.GetEntities();
        foreach (var entity in entities)
        {
            var instance = app.Services.GetRequiredService(typeof(BlossomServerRunner<>).MakeGenericType(entity)) as IBlossomEndpointMapper;
            instance?.MapEndpoints(app);
        }
    }
}