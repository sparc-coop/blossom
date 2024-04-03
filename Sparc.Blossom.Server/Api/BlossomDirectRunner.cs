using Ardalis.Specification;
using Mapster;

namespace Sparc.Blossom.Data;

public class BlossomDirectRunner<T, TEntity>(IRunner<TEntity> serverRunner) 
    : IRunner<T>
    where T : IBlossomEntityProxy<T>
    where TEntity : Entity<string>
{
    public IRunner<TEntity> ServerRunner { get; } = serverRunner;

    public async Task<T?> GetAsync(object id)
    {
        var result = await ServerRunner.GetAsync(id);
        return result == null ? default : Adapt(result);
    }
    public async Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters)
    {
        var results = await ServerRunner.QueryAsync(name, parameters);
        return results.Select(Adapt);
    }

    public async Task ExecuteAsync(object id, string name, params object[] parameters) => 
        await ServerRunner.ExecuteAsync(id, name, parameters);

    public Task OnAsync(object id, string name, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    private T Adapt(TEntity entity)
    {
        var dto = entity.Adapt<T>();
        dto.Runner = this;
        return dto;
    }
}