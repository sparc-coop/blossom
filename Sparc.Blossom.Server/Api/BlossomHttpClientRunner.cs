using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace Sparc.Blossom.Data;

public class BlossomHttpClientRunner<T>(HttpClient client) : IRunner<T> where T : Entity<string>
{
    private HttpClient Client { get; } = client;

    public async Task<T?> GetAsync(object id) => await Client.GetFromJsonAsync<T>(id.ToString());
    public async Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters)
    {
        var request = await Client.PostAsJsonAsync(name, parameters);
        return await request.Content.ReadFromJsonAsync<IEnumerable<T>>() ?? [];
    }

    public async Task ExecuteAsync(object id, string name, params object[] parameters)
    {
        var request = await Client.PutAsJsonAsync($"{id}/{name}", parameters);
        request.EnsureSuccessStatusCode();
    }

    public Task OnAsync(object id, string name, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}