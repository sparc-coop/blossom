using Microsoft.JSInterop;
using System.Reflection;

namespace Sparc.Blossom;

public static class BlossomExtensions
{
    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }

    public static Type? GetAggregate(this Assembly assembly, Type entityType)
        => assembly.GetDerivedTypes(typeof(BlossomAggregate<>).MakeGenericType(entityType)).FirstOrDefault();

}