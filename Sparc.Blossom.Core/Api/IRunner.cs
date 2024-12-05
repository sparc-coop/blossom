namespace Sparc.Blossom.Api;

public interface IRunner<T>
{
    Task<T> Create(params object?[] parameters);
    Task<T> Add<U>(object id, U item);
    Task<T?> Get(object id);
    Task<IEnumerable<T>> ExecuteQuery(string? name = null, params object?[] parameters);
    Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options);
    Task<BlossomAggregateMetadata> Metadata();
    Task Patch<U>(object id, U item);
    Task Execute(object id, string name, params object?[] parameters);
    Task Delete(object id);
    Task Remove<U>(object id, U item);
    Task On(object id, string name, params object?[] parameters);
    Task<T?> Undo(object id, long? revision);
    Task<T?> Redo(object id, long? revision);
}
