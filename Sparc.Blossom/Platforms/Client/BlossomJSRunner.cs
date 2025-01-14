using Microsoft.JSInterop;

namespace Sparc.Blossom;

public class BlossomJsRunner(IJSRuntime js, string jsPath) : IAsyncDisposable
{
    public IJSRuntime Js { get; } = js;
    readonly Lazy<Task<IJSObjectReference>> koriApp = js.Import(jsPath);

    protected async Task<T> InvokeAsync<T>(string identifier, params object[] args)
    {
        var module = await koriApp.Value;
        return await module.InvokeAsync<T>(identifier, args);
    }

    protected async Task InvokeVoidAsync(string identifier, params object[] args)
    {
        var module = await koriApp.Value;
        await module.InvokeVoidAsync(identifier, args);
    }

    public async ValueTask DisposeAsync()
    {
        if (koriApp.IsValueCreated)
        {
            var module = await koriApp.Value;
            await module.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}