namespace Sparc.Blossom;

public static class HttpClientExtensions
{
    public static async Task<TOut?> PostAsJsonAsync<TIn, TOut>(this HttpClient client, string url, TIn model)
    {
        var response = await client.PostAsJsonAsync(url, model);
        return await response.Content.ReadFromJsonAsync<TOut>();
    }
}