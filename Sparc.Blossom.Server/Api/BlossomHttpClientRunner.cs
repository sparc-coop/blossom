namespace Sparc.Blossom.Api;

public class BlossomHttpClientRunner<T>(HttpClient client) : IRunner<T>
{
    private HttpClient Client { get; } = client;

    public async Task<T> CreateAsync(params object?[] parameters) 
        => await PostAsJsonAsync<T>("", parameters);

    public async Task<T?> GetAsync(object id) => await Client.GetFromJsonAsync<T>(id.ToString());
    public async Task<IEnumerable<T>> QueryAsync(string? name = null, params object?[] parameters) 
        => await PostAsJsonAsync<IEnumerable<T>>(name, parameters);

    public async Task ExecuteAsync(object id, string name, params object?[] parameters)
    {
        var request = await Client.PutAsJsonAsync($"{id}/{name}", parameters);
        request.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(object id) => await Client.DeleteAsync(id.ToString());

    public Task OnAsync(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    private async Task<TResult> PostAsJsonAsync<TResult>(string? name = null, params object?[] parameters)
    {
        var request = await Client.PostAsJsonAsync(name ?? "", parameters);
        var result = await request.Content.ReadFromJsonAsync<TResult>();
        return result == null ? throw new Exception("Result is null") : result;
    }

    public async Task<T?> UndoAsync(object id, long? revision) => await PostAsJsonAsync<T?>("_undo", id, revision);
    public async Task<T?> RedoAsync(object id, long? revision) => await PostAsJsonAsync<T?>("_redo", id, revision);
}