using System.Reflection;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomEndpointMapper(Assembly assembly)
{
    public Assembly Assembly { get; } = assembly;

    public void MapEntityEndpoints(IEndpointRouteBuilder endpoints)
    {
        var entities = Assembly.GetEntities();
        foreach (var entity in entities)
        {
            var name = Assembly.GetAggregate(entity)?.Name.ToLower() ?? entity.Name.ToLower();
            GetType().GetMethod("MapEndpoints")!.MakeGenericMethod(entity).Invoke(this, [name, endpoints]);
        }
    }

    public static void MapEndpoints<T>(string name, IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/{name}";
        var group = endpoints.MapGroup(baseUrl);
        group.MapGet("{id}", async (IRunner<T> runner, string id) => await runner.Get(id));
        group.MapPost("", async (IRunner<T> runner, object[] parameters) => await runner.Create(parameters));
        group.MapPost("_undo", async (IRunner<T> runner, string id, long? revision) => await runner.Undo(id, revision));
        group.MapPost("_redo", async (IRunner<T> runner, string id, long? revision) => await runner.Redo(id, revision));
        group.MapGet("_metadata", async (IRunner<T> runner) => await runner.Metadata());
        group.MapPost("_queries/{name}", async (IRunner<T> runner, string name, object[] parameters) => await runner.ExecuteDynamicQuery(name, parameters));
        group.MapPatch("{id}", async (IRunner<T> runner, string id, BlossomPatch patch) => await runner.Patch(id, patch));
        //group.MapPost("_queries", async (IRunner<T> runner, string name, BlossomQueryOptions options, object[] parameters) => await runner.ExecuteQuery(name, options, parameters));
        group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.Execute(id, name, parameters));
        group.MapDelete("{id}", async (IRunner<T> runner, string id) => await runner.Delete(id));
    }
}
