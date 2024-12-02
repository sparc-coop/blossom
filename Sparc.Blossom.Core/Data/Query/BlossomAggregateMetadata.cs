namespace Sparc.Blossom.Api;

public class BlossomAggregateMetadata(Type type)
{
    public string Name { get; } = type.Name;
    public List<BlossomProperty> Properties { get; } = type.GetProperties()
        .Where(p => p.DeclaringType == type && p.SetMethod?.IsPublic == true)
        .Select(x => new BlossomProperty(x.Name, x.PropertyType)).ToList();
}
