using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Sparc.Blossom.Content.OpenAI;

public record JsonSchema(string Type, bool? AdditionalProperties = false)
{
    public JsonSchema(Type type, IEnumerable<PropertyInfo>? properties = null) : this("object")
    {
        properties ??= type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        Properties = properties.ToDictionary(
            p => p.Name,
            p => new JsonSchemaProperty(p)
        );
    }

    [NotMapped]
    public Dictionary<string, JsonSchemaProperty>? Properties { get; set; }
    public List<string>? Required => Properties?.Select(x => x.Key).ToList();
}
