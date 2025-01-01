using System.Reflection;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomEndpointMapper(Assembly assembly)
{
    public Assembly Assembly { get; } = assembly;

    public void MapEntityEndpoints(IEndpointRouteBuilder endpoints)
    {
        var entities = Assembly.GetEntities();
        foreach (var entity in entities)
            GetType().GetMethod("MapEndpoints")!.MakeGenericMethod(entity).Invoke(this, [endpoints]);
    }

    public static void MapEndpoints<T>(IEndpointRouteBuilder endpoints) where T : BlossomEntity
    {
        //var baseUrl = $"/{typeof(T).Name.ToLower()}";
        //var group = endpoints.MapGroup(baseUrl);
        //group.MapGet("{id}", async (BlossomCollectionProxy<T> runner, string id) => await runner.FindAsync(id));
        //group.MapPost("", async (BlossomEntityProxy<T> runner, object[] parameters) => await runner.Create(parameters));
        //// group.MapPost("_undo", async (IRunner<T> runner, string id, long? revision) => await runner.Undo(id, revision));
        //// group.MapPost("_redo", async (IRunner<T> runner, string id, long? revision) => await runner.Redo(id, revision));
        //// group.MapGet("_metadata", async (IRunner<T> runner) => await runner.Metadata());
        //group.MapPost("{name}", async (BlossomCollectionProxy<T> runner, string name, object[] parameters) => await runner.ExecuteQuery(name, parameters));
        //group.MapPatch("{id}", async (IRunner<T> runner, string id, BlossomPatch patch) => await runner.Patch(id, patch));
        ////group.MapGet("{name}_flex", async (IRunner<T> runner, string name, BlossomQueryOptions options, object[] parameters) => await runner.ExecuteQuery(name, options, parameters));
        //group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.Execute(id, name, parameters));
        //group.MapDelete("{id}", async (IRunner<T> runner, string id) => await runner.Delete(id));
    }
}
