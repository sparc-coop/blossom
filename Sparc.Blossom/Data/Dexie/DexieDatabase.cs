using Ardalis.Specification;
using Microsoft.JSInterop;
using System.Reflection;

namespace Sparc.Blossom.Data.Dexie;

public class DexieDatabase(IJSRuntime js)
{
    readonly Lazy<Task<IJSObjectReference>> Dexie = js.Import("./Blossom/Data/Dexie/DexieDatabase.js");
    internal readonly Dictionary<string, List<string>> Repositories = [];
    public IJSObjectReference? Db { get; private set; }

    public async Task InitializeAsync(int version, string? name = null)
    {
        var dexie = await Dexie.Value;

        name ??= Assembly.GetEntryAssembly().GetName().Name?.Split(".").Last()
            ?? "Blossom";

        foreach (var entity in Assembly.GetEntryAssembly().GetEntities())
            RegisterRepository(entity);

        await dexie.InvokeVoidAsync("init", name, Repositories, version);
        Db = await dexie.InvokeAsync<IJSObjectReference>("db");
    }

    public async Task<IJSObjectReference> Set<T>()
    {
        if (Db == null)
            await InitializeAsync(1);

        var dexie = await Dexie.Value;
        var set = await dexie.InvokeAsync<IJSObjectReference>("repository", typeof(T).Name.ToLower());
        return set;
    }

    public async Task<DexieQuery<T>> Query<T>(ISpecification<T> spec) 
    {
        var query = new DexieQuery<T>(this);
        return await query.ApplyAsync(spec);
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
