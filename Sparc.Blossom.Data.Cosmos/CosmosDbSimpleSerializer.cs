using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;
public sealed class CosmosDbSimpleSerializer : CosmosLinqSerializer
{
    private readonly JsonObjectSerializer systemTextJsonSerializer;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer()
    {
        jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = new CamelCaseIdNamingPolicy(),
            MaxDepth = 64
        };
        systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
    }

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer(CosmosSerializationOptions cosmosSerializerOptions) : this()
    {
        if (cosmosSerializerOptions != null)
        {
            jsonSerializerOptions = new()
            {
                DefaultIgnoreCondition = cosmosSerializerOptions.IgnoreNullValues ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
                PropertyNamingPolicy = new CamelCaseIdNamingPolicy(),
                MaxDepth = 64
            };
            systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
        }
    }

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer(JsonSerializerOptions jsonSerializerSettings)
    {
        jsonSerializerOptions = jsonSerializerSettings;
        systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
    }
    

    //
    // Summary:
    //     Convert a Stream to the passed in type.
    //
    // Parameters:
    //   stream:
    //     An open stream that is readable that contains JSON
    //
    // Type parameters:
    //   T:
    //     The type of object that should be deserialized
    //
    // Returns:
    //     The object representing the deserialized stream
    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek
                   && stream.Length == 0)
            {
                return default!;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return (T)systemTextJsonSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    //
    // Summary:
    //     Converts an object to a open readable stream
    //
    // Parameters:
    //   input:
    //     The object to be serialized
    //
    // Type parameters:
    //   T:
    //     The type of object being serialized
    //
    // Returns:
    //     An open readable stream containing the JSON of the serialized object
    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new MemoryStream();
        this.systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }

    public override string SerializeMemberName(MemberInfo memberInfo)
    {
        JsonExtensionDataAttribute jsonExtensionDataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true)!;
        if (jsonExtensionDataAttribute != null)
        {
            return null!;
        }

        JsonPropertyNameAttribute jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true)!;
        if (!string.IsNullOrEmpty(jsonPropertyNameAttribute?.Name))
        {
            return jsonPropertyNameAttribute.Name;
        }

        if (this.jsonSerializerOptions.PropertyNamingPolicy != null)
        {
            return this.jsonSerializerOptions.PropertyNamingPolicy.ConvertName(memberInfo.Name);
        }

        // Do any additional handling of JsonSerializerOptions here.

        return memberInfo.Name;
    }
}
