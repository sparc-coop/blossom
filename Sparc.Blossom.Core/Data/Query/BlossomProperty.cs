
using System.Collections;
using System.Reflection;

namespace Sparc.Blossom.Api;

public class BlossomProperty(PropertyInfo property)
{
    public string Name { get; } = property.Name;
    public string Type { get; } = !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) ? (property.PropertyType.GenericTypeArguments?.First().Name ?? property.PropertyType.Name) : property.PropertyType.Name;
    public bool IsPrimitive { get; } = property.PropertyType.IsPrimitive || property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(decimal);
    public bool IsEnumerable { get; } = !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
    public bool CanRead => property.Name != "SubscriptionId" && property.Name != "Runner" && property.Name != "GenericId";
    public bool CanAdd { get; }
    public bool CanEdit => property.DeclaringType == property.ReflectedType && property.SetMethod?.IsPublic == true;
    public string? EditorType => !CanEdit ? null : Type switch
    {
        "Int32" => "number",
        "Int64" => "number",
        "Decimal" => "number",
        "DateTime" => "date",
        "Boolean" => "checkbox",
        "String" => DistinctPercentage < 0.5M ? (DistinctValues < 5 ? "radio" : DistinctValues < 25 ? "select" : "search") : "text", 
        _ => IsEnumerable ? DistinctPercentage == 1 ? "onetomany" : "manytomany" : "search"
    };
    public int? DistinctValues { get; set; }
    public int? TotalCount { get; set; }
    public decimal? DistinctPercentage => TotalCount == 0 ? 0 : DistinctValues / (decimal?)TotalCount;
    public Dictionary<string, string> AvailableValues { get; set; } = [];

    public void SetAvailableValues(Dictionary<object, int> results)
    {
        TotalCount = results.Values.Sum();
        DistinctValues = results.Count;

        if (results.ContainsKey("<null>"))
            DistinctValues = (DistinctValues - 1) + results["<null>"];
        
            if (DistinctValues < 25)
            AvailableValues = results.ToDictionary(x => x.Key.ToString(), x => $"{x.Key} (Used {x.Value}x)");
    }

    public void SetAvailableValues(int totalCount, Dictionary<string, string> results)
    {
        TotalCount = totalCount;
        DistinctValues = results.Count;
        if (DistinctValues < 25)
            AvailableValues = results.ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());
    }
}
