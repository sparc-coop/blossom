namespace Sparc.Blossom;

public class BlossomAggregateMetadata(Type type)
{
    public string Name { get; } = type.Name;
    public List<BlossomProperty> Properties { get; } = type.GetProperties()
            .Select(x => new BlossomProperty(x))
            .OrderBy(x => x.Name == "Id" ? 0 : 1)
            .ToList();

    public List<BlossomMethod> Methods { get; } = type.GetMethods()
        .Where(x => !x.IsSpecialName && x.DeclaringType == type)
        .Select(x => new BlossomMethod(x)).ToList();
}
