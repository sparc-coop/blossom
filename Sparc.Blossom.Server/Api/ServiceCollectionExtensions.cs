using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var entities = assembly.GetTypes()
            .Where(x => typeof(Entity<>).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
        
        foreach (var entity in entities)
        {
            var instance = app.Services.GetRequiredService(typeof(BlossomServerRunner<>).MakeGenericType(entity)) as IBlossomServerRunner;
            instance?.MapEndpoints(app);
        }
    }
}