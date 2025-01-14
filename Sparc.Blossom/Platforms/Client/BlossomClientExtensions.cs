using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sparc.Blossom;

public static class BlossomClientExtensions
{
    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public static async Task<TResponse?> PostAsJsonAsync<TResponse>(this HttpClient client, string url, object request)
    {
        try
        {
            var response = await client.PostAsJsonAsync(url, request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(result, JsonOptions);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static async Task<TOut?> PostAsJsonAsync<TIn, TOut>(this HttpClient client, string url, TIn model)
    {
        var response = await client.PostAsJsonAsync(url, model);
        return await response.Content.ReadFromJsonAsync<TOut>();
    }

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}
