
using Microsoft.JSInterop;

namespace Sparc.Blossom.Authentication.Passwordless
{
    public class BlossomPasswordlessAuthenticatorClient(IJSRuntime js) : IAsyncDisposable
    {
        readonly Lazy<Task<IJSObjectReference>> PasswordlessClient = new(() => 
            js.InvokeAsync<IJSObjectReference>(
                "import", 
                "./content/Sparc.Blossom.Authentication.Passwordless/Passwordless.razor.js")
            .AsTask());

        public async Task InitializeAsync(string apiKey)
        {
            var client = await PasswordlessClient.Value;
            await client.InvokeVoidAsync("initialize", apiKey);
        }

        public async Task RegisterAsync(string email)
        {
            var client = await PasswordlessClient.Value;
            await client.InvokeVoidAsync("register", email);
        }

        public async Task LoginAsync(string alias)
        {
            var client = await PasswordlessClient.Value;
            await client.InvokeVoidAsync("login", alias);
        }

        public async ValueTask DisposeAsync()
        {
            if (PasswordlessClient.IsValueCreated)
            {
                var module = await PasswordlessClient.Value;
                await module.DisposeAsync();
            }
        }
    }
}
