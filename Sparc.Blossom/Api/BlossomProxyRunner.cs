using Ardalis.Specification;
using Mapster;

namespace Sparc.Blossom;

public class BlossomProxyRunner<T, TEntity>(IRunner<TEntity> aggregate, BlossomHubProxy realtime) 
    : IRunner<T>
    where T : IBlossomEntityProxy<T>, IBlossomEntityProxy
{
    public IRunner<TEntity> Aggregate { get; } = aggregate;
    public BlossomHubProxy Realtime { get; } = realtime;

    public async Task<T> Create(params object?[] parameters)
    {
        var result = await Aggregate.Create(parameters);
        return Adapt(result);
    }

    public async Task<T?> Get(object id)
    {
        var result = await Aggregate.Get(id);
        return result == null ? default : await AdaptAndWatch(result);
    }

    public async Task<IEnumerable<T>> ExecuteQuery(string? name = null, params object?[] parameters)
    {
        var results = await Aggregate.ExecuteQuery(name, parameters);
        var dtos = results.Select(Adapt);
        await Realtime.Watch((IEnumerable<IBlossomEntityProxy>)dtos);
        return dtos;
    }

    public async Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options)
    {
        var results = await Aggregate.ExecuteQuery(options);
        var dtos = results.Items.Select(Adapt);
        await Realtime.Watch((IEnumerable<IBlossomEntityProxy>)dtos);
        return new BlossomQueryResult<T>(dtos, results.TotalCount);
    }

    public async Task<BlossomAggregateMetadata> Metadata() => await Aggregate.Metadata();

    public async Task Patch(object id, BlossomPatch changes)
    {
        await Aggregate.Patch(id, changes);
    }

    public async Task<T> Execute(object id, string name, params object?[] parameters)
    {
        var result = await Aggregate.Execute(id, name, parameters);
        return Adapt(result);
    }

    public async Task<TResponse?> ExecuteQuery<TResponse>(string name, params object?[] parameters)
    {
        var result = await Aggregate.ExecuteQuery<TResponse?>(name, parameters);
        return result;
    }

    public async Task<T> Update(T entity)
    {
        var existing = await Aggregate.Get(entity.GenericId) 
            ?? throw new Exception($"Entity with id {entity.GenericId} not found.");
        
        var changes = new BlossomPatch(existing, entity);
        await Aggregate.Patch(entity.GenericId, changes);

        return await Get(entity.GenericId)!;
    }

    public async Task Delete(object id) => await Aggregate.Delete(id);

    public Task On(object id, string name, params object?[] parameters)
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

    public async Task<T?> Undo(object id, long? revision)
    {
        var result = await Aggregate.Undo(id, revision);
        return result == null ? default : Adapt(result);
    }

    public async Task<T?> Redo(object id, long? revision)
    {
        var result = await Aggregate.Redo(id, revision);
        return result == null ? default : Adapt(result);
    }
}