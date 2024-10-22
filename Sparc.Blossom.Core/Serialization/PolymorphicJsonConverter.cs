using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;

namespace Sparc.Blossom.Core.Serialization
{
    public class PolymorphicJsonConverter<TBase> : JsonConverter<TBase> where TBase : class
    {
        private const string DiscriminatorPropertyName = "$type";

        public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var type = value.GetType();
            writer.WriteStartObject();

            writer.WriteString(DiscriminatorPropertyName, type.FullName);

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead)
                {
                    var propValue = property.GetValue(value);
                    writer.WritePropertyName(property.Name);

                    JsonSerializer.Serialize(writer, propValue, propValue?.GetType() ?? property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }

        public override TBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null!;
            }

            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty(DiscriminatorPropertyName, out var typeProperty))
            {
                throw new JsonException($"Missing {DiscriminatorPropertyName} property for polymorphic deserialization.");
            }

            var typeName = typeProperty.GetString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("The type discriminator was null or empty.");
            }

            var actualType = ResolveType(typeName);
            if (actualType == null)
            {
                throw new JsonException($"Unable to resolve type: {typeName}");
            }

            var jsonObject = root.GetRawText();
            return (TBase?)JsonSerializer.Deserialize(jsonObject, actualType, options)
                   ?? throw new JsonException($"Deserialization into {actualType} failed.");
        }

        private static Type? ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }

}
