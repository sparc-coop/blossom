namespace Sparc.Blossom;

public class BlossomAggregateProxy<T>(IRunner<T> runner)
{
    public IRunner<T> Runner { get; } = runner;

    public async Task Delete(object id) => await Runner.Delete(id);
    public async Task Delete(T entity)
    {
        var id = (entity as IBlossomEntityProxy)?.GenericId;
        if (id != null)
            await Runner.Delete(id);
    }

    public async Task Update(T entity) => await Runner.Update(entity);
    public async Task<T?> Get(object id) => await Runner.Get(id);
    public async Task<BlossomQueryResult<T>> Query(BlossomQueryOptions options) => await Runner.ExecuteQuery(options);
    public async Task<BlossomAggregateMetadata> Metadata() => await Runner.Metadata();
    public async Task<T?> Undo(object id, long? revision) => await Runner.Undo(id, revision);
    public async Task<T?> Redo(object id, long? revision) => await Runner.Redo(id, revision);
}
