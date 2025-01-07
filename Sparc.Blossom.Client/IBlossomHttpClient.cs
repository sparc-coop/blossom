using Refit;

namespace Sparc.Blossom;

public interface IBlossomHttpClient<T>
{
    [Get("/{id}")]
    Task<T> Get(string id);

    [Get("_metadata")]
    Task<BlossomAggregateMetadata> Metadata();

    [Post("")]
    Task<T> Create([Body] object[] parameters);

    [Post("/_queries")]
    Task<BlossomQueryResult<T>> ExecuteQuery([Body] BlossomQueryOptions options);

    [Post("/_queries/{name}")]
    Task<IEnumerable<T>> ExecuteQuery(string name, [Body] params object[] parameters);

    [Post("/_queries/{name}")]
    Task<TResponse?> ExecuteQuery<TResponse>(string name, object?[] parameters);
    
    [Patch("{id}")]
    Task Patch(string id, BlossomPatch patch);

    [Put("{id}/{name}")]
    Task<T> Execute(string id, string name, [Body] params object[] parameters);

    [Delete("{id}")]
    Task Delete(string id);

    [Post("_undo")]
    Task<T> Undo(string id, long? revision);

    [Post("_redo")]
    Task<T> Redo(string id, long? revision);
}
