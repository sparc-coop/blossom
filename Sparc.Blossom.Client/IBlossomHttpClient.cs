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

    [Post("{name}")]
    Task<IEnumerable<T>> ExecuteQuery(string name, [Body] object[] parameters);

    [Post("/_query")]
    Task<BlossomQueryResult<T>> ExecuteQuery([Body] BlossomQueryOptions options);

    [Patch("{id}")]
    Task Patch(string id, BlossomPatch patch);

    [Put("{id}/{name}")]
    Task Execute(string id, string name, [Body] object[] parameters);

    [Delete("{id}")]
    Task Delete(string id);

    [Post("_undo")]
    Task<T> Undo(string id, long? revision);

    [Post("_redo")]
    Task<T> Redo(string id, long? revision);
}
