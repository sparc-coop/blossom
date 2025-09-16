using System.Reflection;

namespace Sparc.Blossom;

public record JsonSchema(string Type, bool? AdditionalProperties = false)
{
    public JsonSchema() : this("object")
    {
    }
    
    public JsonSchema(Type type, IEnumerable<PropertyInfo>? properties = null) : this("object")
    {
        properties ??= type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        Properties = properties
            .Where(x => x.Name != "GenericId")
            .ToDictionary(
            p => p.Name,
            p => (object)(JsonSchemaProperty.JsonType(p.PropertyType) == "object" ? new JsonSchema(p.PropertyType) : new JsonSchemaProperty(p))
        );
    }

    public Dictionary<string, object>? Properties { get; set; }
    public List<string>? Required => Properties?.Select(x => x.Key).ToList();
}
