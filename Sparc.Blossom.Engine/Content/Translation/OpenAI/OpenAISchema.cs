using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content.OpenAI;
public record OpenAISchema
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool Strict { get; set; }
    public JsonSchema Schema { get; set; } = new("object");

    public OpenAISchema(Type type)
    {
        Name = type.Name;
        Description = type.Name;
        Strict = true;
        Schema = new(type);
    }

    static readonly JsonSerializerOptions serializerOptions = new(JsonSerializerOptions.Web) { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    internal BinaryData ToBinary() => BinaryData.FromString(ToString());

    public override string ToString()
    {
        return JsonSerializer.Serialize(Schema, serializerOptions);
    }

    public static string Format(string type) => type switch
    {
        //"singleselect" => "Use a single value from the following list of valid values: " + string.Join(", ", ValidValues),
        //"multiselect" => "Use one or more values from the following list of valid values: " + string.Join(", ", ValidValues),
        "date" => "Use ISO 8601 to format this value.",
        "money" => "Format to two decimal places.",
        "freetext" => "If the answer is not tiny, format as markdown.",
        _ => ""
    };

}

