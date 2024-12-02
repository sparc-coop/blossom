namespace Sparc.Blossom.Api;

public class BlossomAggregateMetadata
{
    public BlossomAggregateMetadata(Type type)
    {
        Name = type.Name;

        ReadProperties = type.GetProperties()
            .Where(x => x.Name != "SubscriptionId" && x.Name != "Runner")
            .OrderBy(x => x.Name == "Id" ? 0 : 1)
            .Select(x => new BlossomProperty(x)).ToList();

        EditProperties = type.GetProperties()
            .Where(p => p.DeclaringType == type && p.SetMethod?.IsPublic == true)
            .Select(x => new BlossomProperty(x)).ToList();
    }

    public string Name { get; }
    public List<BlossomProperty> ReadProperties { get; }
    public List<BlossomProperty> EditProperties { get; }
}
