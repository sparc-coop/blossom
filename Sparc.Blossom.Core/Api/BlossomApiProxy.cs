namespace Sparc.Blossom;

public abstract class BlossomApiProxy : IBlossomApi
{
    public BlossomAggregateProxy<T>? Aggregate<T>()
        => GetType().GetProperties().FirstOrDefault(x => typeof(BlossomAggregateProxy<T>).IsAssignableFrom(x.PropertyType))?.GetValue(this) as BlossomAggregateProxy<T>;

    public Type? Entity(string aggregateName) =>
        GetType().GetProperties().FirstOrDefault(x => x.Name == aggregateName)?.PropertyType.BaseType?.GenericTypeArguments[0];

    public async Task<BlossomAggregateMetadata> Metadata<T>()
    {
        var aggregate = Aggregate<T>();
        if (aggregate == null)
            return new BlossomAggregateMetadata(typeof(T));
        
        return await aggregate.Metadata();
    }
}
