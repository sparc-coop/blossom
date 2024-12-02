namespace Sparc.Blossom.Api;

public class BlossomApiContext<T>(IRunner<T> runner)
{
    public IRunner<T> Runner { get; } = runner;


    public async Task Delete(object id) => await Runner.Delete(id);
    public async Task<T?> Get(object id) => await Runner.Get(id);
    public async Task<BlossomAggregateMetadata> Metadata() => await Runner.Metadata();
    public async Task<BlossomQueryResult<T>> Query(BlossomQueryOptions options) => await Runner.ExecuteQuery(options);
    public async Task<T?> Undo(object id, long? revision) => await Runner.Undo(id, revision);
    public async Task<T?> Redo(object id, long? revision) => await Runner.Redo(id, revision);
}
