using Microsoft.JSInterop;

namespace Sparc.Kori;

public class KoriJsEngine(IJSRuntime js) : IAsyncDisposable
{
    public IJSRuntime Js { get; } = js;
    readonly Lazy<Task<IJSObjectReference>> Value = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Kori/KoriApp.razor.js").AsTask());

    public async Task<T> InvokeAsync<T>(string identifier, params object[] args)
    {
        var module = await Value.Value;
        return await module.InvokeAsync<T>(identifier, args);
    }

    public async Task InvokeVoidAsync(string identifier, params object[] args)
    {
        var module = await Value.Value;
        await module.InvokeVoidAsync(identifier, args);
    }

    public async ValueTask DisposeAsync()
    {
        if (Value.IsValueCreated)
        {
            var module = await Value.Value;
            await module.DisposeAsync();
        }
    }
}

