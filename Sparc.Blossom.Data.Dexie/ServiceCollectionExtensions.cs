using Microsoft.JSInterop;

namespace Sparc.Blossom.Data;

public static partial class ServiceCollectionExtensions
{
    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}
