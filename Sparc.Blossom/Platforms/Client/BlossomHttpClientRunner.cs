namespace Sparc.Blossom;

public class BlossomHttpClientRunner<T>(IBlossomHttpClient<T> client) : IRunner<T>
{
    private IBlossomHttpClient<T> Client { get; } = client;

    public async Task<T> Create(params object?[] parameters)
        => await Client.Create(parameters);

    public async Task<T?> Get(object id)
        => await Client.Get(id.ToString()!);

    public async Task<IEnumerable<T>> ExecuteQuery(string name, params object?[] parameters)
        => await Client.ExecuteQuery(name, parameters);

    public async Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options)
        => await Client.ExecuteQuery(options);

    public async Task<TResponse?> ExecuteQuery<TResponse>(string name, params object?[] parameters)
        => await Client.ExecuteQuery<TResponse>(name, parameters);

    public async Task<BlossomAggregateMetadata> Metadata()
        => await Client.Metadata();

    public async Task Patch(object id, BlossomPatch changes)
        => await Client.Patch(id.ToString()!, changes);

    public async Task<T> Execute(object id, string name, params object?[] parameters)
        => await Client.Execute(id.ToString()!, name, parameters);

    public async Task Delete(object id)
        => await Client.Delete(id.ToString()!);

    public Task On(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> Undo(object id, long? revision)
        => await Client.Undo(id.ToString()!, revision);

    public async Task<T?> Redo(object id, long? revision)
        => await Client.Redo(id.ToString()!, revision);
}