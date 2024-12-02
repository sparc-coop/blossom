
using System.Collections;

namespace Sparc.Blossom.Api;

public class BlossomProperty(string name, Type type)
{
    public string Name { get; } = name;
    public string Type { get; } = !type.IsPrimitive && type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) ? (type.GenericTypeArguments?.First().Name ?? type.Name) : type.Name;
    public bool IsPrimitive { get; } = type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal);
    public bool IsEnumerable { get; } = !type.IsPrimitive && type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    public string EditorType => Type switch
    {
        "Int32" => "number",
        "Int64" => "number",
        "Decimal" => "number",
        "DateTime" => "date",
        "Boolean" => "checkbox",
        "String" => DistinctValues < 5 ? "radio" : DistinctValues < 25 ? "select" : DistinctPercentage < 0.5M ? "search" : "text", 
        _ => IsEnumerable ? "relationship" : "search"
    };
    public int? DistinctValues { get; set; }
    public int? TotalCount { get; set; }
    public decimal? DistinctPercentage => TotalCount == 0 ? 0 : DistinctValues / (decimal?)TotalCount;
    public Dictionary<string, string> AvailableValues { get; set; } = [];

    public void SetAvailableValues(Dictionary<object, int> results)
    {
        TotalCount = results.Values.Sum();
        DistinctValues = results.Count;
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
