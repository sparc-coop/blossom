using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Sparc.Blossom;

public static class BlossomExtensions
{
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return EntityFrameworkQueryableExtensions.ToListAsync(queryable);
    }

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}