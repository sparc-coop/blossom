using Sparc.Blossom.Data;

namespace Sparc.Blossom.Api;

public interface IRunner<T>
{
    Task<T> CreateAsync(params object?[] parameters);
    Task<T?> GetAsync(object id);
    Task<IEnumerable<T>> QueryAsync(string? name = null, params object?[] parameters);
    Task<BlossomQueryResult<T>> FlexQueryAsync(string name, BlossomQueryOptions options, params object?[] parameters);
    Task ExecuteAsync(object id, string name, params object?[] parameters);
    Task DeleteAsync(object id);
    Task OnAsync(object id, string name, params object?[] parameters);
    Task<T?> UndoAsync(object id, long? revision);
    Task<T?> RedoAsync(object id, long? revision);
}
