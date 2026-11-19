using Microsoft.JSInterop;

namespace Sparc.Blossom;

public static class JsExtensions
{
    public static async ValueTask<T> SafeInvokeAsync<T>(this IJSRuntime js, string identifier, params object?[]? args)
    {
        try
        {
            return await js.InvokeAsync<T>(identifier, args);
        }
        catch
        {
            Console.WriteLine($"JSInterop call failed: {identifier}");
            return default!;
        }
    }

    public static async ValueTask SafeInvokeVoidAsync(this IJSRuntime js, string identifier, params object?[]? args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
        }
        catch
        {
            Console.WriteLine($"JSInterop call failed: {identifier}");
        }
    }


    public static async ValueTask<T> SafeInvokeAsync<T>(this IJSObjectReference js, string identifier, params object?[]? args)
    {
        try
        {
            return await js.InvokeAsync<T>(identifier, args);
        }
        catch
        {
            Console.WriteLine($"JSInterop call failed: {identifier}");
            return default!;
        }
    }

    public static async ValueTask SafeInvokeVoidAsync(this IJSObjectReference js, string identifier, params object?[]? args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
        }
        catch
        {
            Console.WriteLine($"JSInterop call failed: {identifier}");
        }
    }
}
