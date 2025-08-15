using Ardalis.Specification;
using Microsoft.JSInterop;
using System.Reflection;

namespace Sparc.Blossom;

public class DexieDatabase(IJSRuntime js)
{
    readonly Lazy<Task<IJSObjectReference>> Dexie = js.Import("./Data/DexieDatabase.js");
    internal readonly Dictionary<string, List<string>> Repositories = [];
    public IJSObjectReference? Db { get; private set; }

    public async Task InitializeAsync(int version, string? name = null)
    {
        var dexie = await Dexie.Value;

        name ??= Assembly.GetCallingAssembly().GetName().Name?.Split(".").Last()
            ?? "Blossom";

        foreach (var entity in Assembly.GetCallingAssembly().GetEntities())
            RegisterRepository(entity);

        await dexie.InvokeVoidAsync("DexieDatabase.init", name, Repositories, version);
        Db = await dexie.InvokeAsync<IJSObjectReference>("DexieDatabase.db");
    }

    public async Task<IJSObjectReference> Set<T>()
    {
        if (Db == null)
            throw new Exception("Database not initialized. Call InitializeAsync first.");

        var set = await Db.InvokeAsync<IJSObjectReference>("set", typeof(T).Name.ToLower());
        return set;
    }

    public async Task<IQueryable<T>> Query<T>(ISpecification<T> spec) 
    {
        var query = new DexieQuery<T>(this);
        var result = await query.ExecuteAsync(spec);
        return result.AsQueryable();
    }

    void RegisterRepository(Type entity)
    {
        var typeName = entity.Name.ToLower();

        var indexes = entity.GetProperties()
            .Where(x => x.GetCustomAttribute<BlossomIndexAttribute>() != null)
            .Select(x => x.Name.ToLower())
            .OrderBy(x => x)
            .ToList();

        if (!Repositories.ContainsKey(typeName))
            Repositories[typeName] = ["id", .. indexes];
    }
}
