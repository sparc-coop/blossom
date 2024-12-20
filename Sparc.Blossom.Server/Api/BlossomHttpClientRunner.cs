using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Api;

public class BlossomHttpClientRunner<T>(HttpClient client) : IRunner<T> where T : BlossomEntity<string>
{
    private HttpClient Client { get; } = client;

    public async Task<T> Create(params object?[] parameters) 
        => await PostAsJsonAsync<T>("", parameters);

    public async Task<T?> Get(object id) => await Client.GetFromJsonAsync<T>(id.ToString());

    public async Task<IEnumerable<T>> ExecuteQuery(string? name = null, params object?[] parameters) 
        => await PostAsJsonAsync<IEnumerable<T>>(name, parameters);

    public async Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options)
    => await PostAsJsonAsync<BlossomQueryResult<T>>("_query", options);

    public async Task<BlossomAggregateMetadata> Metadata()
        => await Client.GetFromJsonAsync<BlossomAggregateMetadata>("_metadata") ?? new(typeof(T));

    public async Task Patch(object id, BlossomPatch changes)
        => await Client.PatchAsJsonAsync($"{id}", changes);

    public async Task Execute(object id, string name, params object?[] parameters)
    {
        var request = await Client.PutAsJsonAsync($"{id}/{name}", parameters);
        request.EnsureSuccessStatusCode();
    }

    public async Task Delete(object id) => await Client.DeleteAsync(id.ToString());

    public Task On(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    private async Task<TResult> PostAsJsonAsync<TResult>(string? name = null, params object?[] parameters)
    {
        var request = await Client.PostAsJsonAsync(name ?? "", parameters);
        var result = await request.Content.ReadFromJsonAsync<TResult>();
        return result == null ? throw new Exception("Result is null") : result;
    }

    public async Task<T?> Undo(object id, long? revision) => await PostAsJsonAsync<T?>("_undo", id, revision);
    public async Task<T?> Redo(object id, long? revision) => await PostAsJsonAsync<T?>("_redo", id, revision);
}