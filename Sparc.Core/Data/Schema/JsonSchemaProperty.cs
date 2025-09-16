using System.ComponentModel;
using System.Reflection;

namespace Sparc.Blossom;
public record JsonSchemaProperty
{
    internal JsonSchemaProperty() 
    { 
        Type = ["string", "null"];
    }
    
    public JsonSchemaProperty(PropertyInfo property) : this(property.PropertyType)
    {
        var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
        Description = $"{description}{Description}";
    }

    public JsonSchemaProperty(Type type)
    {
        if (type == typeof(object))
            Type = ["string", "integer", "number", "boolean", "null"];
        else
            Type = [JsonType(type), "null"];

        if (JsonType(type) == "array")
            Items = new JsonSchema(JsonType(type.GenericTypeArguments[0]));
        else if (type.IsGenericType && type.GenericTypeArguments[0] != null)
            Items = new JsonSchema(type.GenericTypeArguments[0]);

        if (type == typeof(DateTime) || type == typeof(DateTime?))
            Description = " Format in ISO 8601.";
    }

    private static string JsonType(Type type)
    {
        // Get underlying type if property type is nullable
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            type = Nullable.GetUnderlyingType(type)!;

        return type switch
        {
            Type t when t == typeof(List<string>) => "array",
            Type t when t == typeof(List<double>) => "array",
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) => "integer",
            Type t when t == typeof(long) => "integer",
            Type t when t == typeof(float) => "number",
            Type t when t == typeof(double) => "number",
            Type t when t == typeof(decimal) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t == typeof(DateTime) => "string",
            _ => "string"
        };
    }

    public void ToMultiple()
    {
        var type = Type.First();
        Type = ["array", "null"];
        Items = new JsonSchema(type);
    }

    public List<string> Type { get; set; } = [];
    public string? Format { get; set; }
    public List<string>? Enum { get; set; }
    public JsonSchema? Items { get; set; }
    public string? Description { get; set; }
}