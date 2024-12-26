using Microsoft.JSInterop;

namespace Sparc.Blossom.Kori;

public static class KoriExtensions
{
    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}
