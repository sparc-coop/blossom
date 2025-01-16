namespace Sparc.Blossom;

public interface IRunner
{
    Task Patch(object id, BlossomPatch changes);
    Task Delete(object id);
    Task On(object id, string name, params object?[] parameters);
}

public interface IRunner<T> : IRunner
{
    Task<T> Create(params object?[] parameters);
    Task<T?> Get(object id);
    Task<T> Execute(object id, string name, params object?[] parameters);
    Task<IEnumerable<T>> ExecuteQuery(string name, params object?[] parameters);
    Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options);
    Task<TResponse?> ExecuteQuery<TResponse>(string name, params object?[] parameters);
    Task<T?> Undo(object id, long? revision);
    Task<T?> Redo(object id, long? revision);
}
