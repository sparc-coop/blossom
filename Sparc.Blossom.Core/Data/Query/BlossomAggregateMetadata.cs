namespace Sparc.Blossom.Api;

public class BlossomAggregateMetadata(Type type)
{
    public string Name { get; } = type.Name;
    public List<BlossomProperty> Properties { get; } = type.GetProperties().Select(x => new BlossomProperty(x))
            .OrderBy(x => x.Name == "Id" ? 0 : 1)
            .ToList();
}
