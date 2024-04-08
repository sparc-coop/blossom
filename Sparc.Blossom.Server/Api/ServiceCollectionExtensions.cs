using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static IEnumerable<Type> GetDerivedTypes(this Assembly assembly, Type baseType)
        => assembly.GetTypes().Where(x => x.BaseType?.IsGenericType == true && x.BaseType.GetGenericTypeDefinition() == baseType);

    public static IEnumerable<Type> GetEntities(this Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomEntity<>));

    public static IEnumerable<Type> GetDtos(this Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomApiContext<>))
            .Select(x => x.BaseType!.GetGenericArguments().First())
            .Distinct();

    public static void RegisterBlossomContexts(this WebApplicationBuilder builder, Assembly assembly)
    {
        var apis = assembly.GetDerivedTypes(typeof(BlossomApiContext<>));
        foreach (var api in apis)
            builder.Services.AddScoped(api);    
        
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
        using var scope = app.Services.CreateScope();
        foreach (var entity in entities)
        {
            var instance = scope.ServiceProvider.GetRequiredService(typeof(IRunner<>).MakeGenericType(entity)) as IBlossomEndpointMapper;
            instance?.MapEndpoints(app);
        }
    }
}