//namespace Sparc.Blossom;

//public abstract class BlossomApiProxy : IBlossomApi
//{
//    public BlossomCollectionProxy<T>? Aggregate<T>()
//        => GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsAssignableTo(typeof(BlossomCollectionProxy<T>)))?.GetValue(this) as BlossomCollectionProxy<T>;

//    public Type? Entity(string aggregateName) =>
//        GetType().GetProperties().FirstOrDefault(x => x.Name == aggregateName)?.PropertyType.BaseType?.GenericTypeArguments[0];

//    public async Task<BlossomAggregateMetadata> Metadata<T>()
//    {
//        var aggregate = Aggregate<T>();
//        if (aggregate == null)
//            return new BlossomAggregateMetadata(typeof(T));
        
//        return await aggregate.Metadata();
//    }
//}
