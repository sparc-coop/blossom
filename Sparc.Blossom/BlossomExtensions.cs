using Microsoft.JSInterop;

namespace Sparc.Blossom;

public static class BlossomExtensions
{
    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }

    public static Dictionary<Type, Type> GetDtos(this Assembly assembly)
    {
        var entities = assembly.GetEntities();
        
        var dtos = assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

        return dtos
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null)
            .ToDictionary(x => x.Key, x => x.Value!);
    }

    public static IEnumerable<Type> GetAggregates(this Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomAggregate<>));

    public static Type? GetAggregateProxy(this Assembly assembly, Type entityType)
    => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>).MakeGenericType(entityType)).FirstOrDefault();
}