
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Sparc.Blossom;

public class BlossomProperty
{
    public BlossomProperty(PropertyInfo property)
    {
        Property = property;
        Name = property.Name;
        Type = !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) ? (property.PropertyType.GenericTypeArguments?.First().Name ?? property.PropertyType.Name) : property.PropertyType.Name;
        IsPrimitive = property.PropertyType.IsPrimitive || property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(decimal);
        IsEnumerable = !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
        CanRead = property.Name != "SubscriptionId" && property.Name != "Runner" && property.Name != "GenericId";
        CanEdit = property.DeclaringType == property.ReflectedType && property.SetMethod?.IsPublic == true;
    }

    public BlossomProperty(ParameterInfo parameter)
    {
        Name = parameter.Name;
        Type = parameter.ParameterType.Name;
        IsPrimitive = parameter.ParameterType.IsPrimitive || parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(DateTime) || parameter.ParameterType == typeof(decimal);
        IsEnumerable = !parameter.ParameterType.IsPrimitive && parameter.ParameterType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(parameter.ParameterType);
        CanRead = true;
        CanEdit = true;
    }

    PropertyInfo? Property { get; }
    public string Name { get; }
    public string FriendlyName =>
        Regex.Replace(Name, @"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");
    public string Type { get; }
    public bool IsPrimitive { get; }
    public bool IsEnumerable { get; }
    public bool CanRead { get; }
    public bool CanEdit { get; }
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
    public Dictionary<string, dynamic> AvailableValues { get; set; } = [];
    public List<T> GetAvailableValues<T>() => AvailableValues.Values.Cast<T>().ToList();
    public object GetAvailableValues(Type type) => GetType().GetMethod(nameof(GetAvailableValues))!.MakeGenericMethod(type).Invoke(Property, null);

    public object? _value;
    public object? Value(object entity) => _value ?? (CanRead ? Property?.GetValue(entity) : null);
    public string? ToString(object? entity)
    {
        if (entity == null)
            return null;

        var value = Value(entity);
        if (value is IList list)
            return list.Count.ToString();
        
        return value?.ToString();
    }

    public void SetValue(object entity, object? value)
    {
        if (Property == null)
            _value = value;
        else if (CanEdit) 
            Property.SetValue(entity, value);
    }


    public void SetAvailableValues(Dictionary<object, int> results)
    {
        TotalCount = results.Values.Sum();
        DistinctValues = results.Count;

        if (results.ContainsKey("<null>"))
            DistinctValues = (DistinctValues - 1) + results["<null>"];

        if (DistinctValues < 25)
            AvailableValues = results.ToDictionary(x => x.Key.ToString(), x => (dynamic)$"{x.Key} (Used {x.Value}x)");
    }

    public void SetAvailableValues(int totalCount, Dictionary<string, dynamic> results)
    {
        TotalCount = totalCount;
        DistinctValues = results.Count;
        if (DistinctValues < 25)
            AvailableValues = results.ToDictionary(x => x.Key.ToString(), x => x.Value);
    }
}
