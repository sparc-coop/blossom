namespace Sparc.Blossom.Api;

public abstract class BlossomApiProxy : IBlossomApi
{
    public BlossomAggregateProxy<T> Aggregate<T>()
        => GetType().GetProperties().First(x => x.PropertyType.IsAssignableTo(typeof(BlossomAggregateProxy<T>))).GetValue(this) as BlossomAggregateProxy<T>
        ?? throw new Exception($"Aggregate {typeof(T).Name} not found.");
}
