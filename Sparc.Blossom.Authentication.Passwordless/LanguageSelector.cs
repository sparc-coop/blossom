using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Sparc.Blossom.Authentication.Passwordless;
public class LanguageSelector(IJSRuntime js): IAsyncDisposable
{
    public record Language(string Id, string DisplayName, string NativeName, bool IsRightToLeft);
    readonly Lazy<Task<IJSObjectReference>> LoginJs = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Blossom.Passwordless/GlobalLogin.razor.js").AsTask());

    public async Task InitializeAsync(ComponentBase component)
    {
        await LoginJs.Value;
    }

    public static async Task<string> GetLanguageAsync()
    {
        var languages = await GetLanguagesAsync();
        var language = languages.FirstOrDefault(x => x.Id == "en");
        return language?.DisplayName ?? "English";
    }

    public static async Task<List<Language>> GetLanguagesAsync()
    {
        var client = new HttpClient { BaseAddress = new Uri("https://ibis-web-kori.azurewebsites.net/") };
        return await client.GetFromJsonAsync<List<Language>>("publicapi/Languages")
               ?? new List<Language>();
    }

    public async ValueTask DisposeAsync()
    {
        if (LoginJs.IsValueCreated)
        {
            var module = await LoginJs.Value;
            await module.DisposeAsync();
        }
    }
}
