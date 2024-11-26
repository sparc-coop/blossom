using Ardalis.Specification;
using Mapster;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Api;

public class BlossomDirectRunner<T, TEntity>(IRunner<TEntity> serverRunner, BlossomRealtimeContext realtime) 
    : IRunner<T>
    where T : IBlossomProxy<T>, IBlossomEntityProxy
{
    public IRunner<TEntity> ServerRunner { get; } = serverRunner;
    public BlossomRealtimeContext Realtime { get; } = realtime;

    public async Task<T> CreateAsync(params object?[] parameters)
    {
        var result = await ServerRunner.CreateAsync(parameters);
        return Adapt(result);
    }

    public async Task<T?> GetAsync(object id)
    {
        var result = await ServerRunner.GetAsync(id);
        return result == null ? default : await AdaptAndWatch(result);
    }

    public async Task<IEnumerable<T>> QueryAsync(string? name = null, params object?[] parameters)
    {
        var results = await ServerRunner.QueryAsync(name, parameters);
        var dtos = results.Select(Adapt);
        await Realtime.Watch((IEnumerable<IBlossomEntityProxy>)dtos);
        return dtos;
    }

    public async Task<BlossomQueryResult<T>> FlexQueryAsync(string name, BlossomQueryOptions options, params object?[] parameters)
    {
        var results = await ServerRunner.FlexQueryAsync(name, options, parameters);
        return new BlossomQueryResult<T>(results.Items.Select(Adapt), results.TotalCount);
    }

    public async Task ExecuteAsync(object id, string name, params object?[] parameters) => 
        await ServerRunner.ExecuteAsync(id, name, parameters);

    public async Task DeleteAsync(object id) => await ServerRunner.DeleteAsync(id);

    public Task OnAsync(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    private T Adapt(TEntity entity)
    {
        var dto = entity.Adapt<T>();
        dto.Runner = this;
        return dto;
    }

    private async Task<T> AdaptAndWatch(TEntity entity)
    {
        var dto = Adapt(entity);
        await Realtime.Watch(dto);
        return dto;
    }

    public async Task<T?> UndoAsync(object id, long? revision)
    {
        var result = await ServerRunner.UndoAsync(id, revision);
        return result == null ? default : Adapt(result);
    }

    public async Task<T?> RedoAsync(object id, long? revision)
    {
        var result = await ServerRunner.RedoAsync(id, revision);
        return result == null ? default : Adapt(result);
    }
}