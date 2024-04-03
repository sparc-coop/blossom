using Ardalis.Specification;
using Mapster;

namespace Sparc.Blossom.Data;

public class BlossomDirectRunner<T, TEntity>(BlossomServerRunner<TEntity> serverRunner) 
    : IRunner<T> where TEntity : Entity<string>
{
    public BlossomServerRunner<TEntity> ServerRunner { get; } = serverRunner;

    public async Task<T?> GetAsync(object id) => (await ServerRunner.GetAsync(id)).Adapt<T>();
    public async Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters)
    {
        var results = await ServerRunner.QueryAsync(name, parameters);
        return results.Select(x => x.Adapt<T>());
    }

    public async Task ExecuteAsync(object id, string name, params object[] parameters) => 
        await ServerRunner.ExecuteAsync(id, name, parameters);

    public Task OnAsync(object id, string name, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}