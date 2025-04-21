using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Text;

namespace Sparc.Blossom.Data;
public sealed class CosmosDbSimpleSerializer : CosmosSerializer
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly JsonSerializerSettings? SerializerSettings;

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer()
    {
        SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCaseIdContractResolver(),
            MaxDepth = 64
        }; ;
    }

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer(CosmosSerializationOptions cosmosSerializerOptions)
    {
        if (cosmosSerializerOptions == null)
        {
            SerializerSettings = null;
            return;
        }

        JsonSerializerSettings serializerSettings = new()
        {
            NullValueHandling = cosmosSerializerOptions.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
            Formatting = cosmosSerializerOptions.Indented ? Formatting.Indented : Formatting.None,
            ContractResolver = new CamelCaseIdContractResolver(),
            MaxDepth = 64
        };
        SerializerSettings = serializerSettings;
    }

    //
    // Summary:
    //     Create a serializer that uses the JSON.net serializer
    //
    // Remarks:
    //     This is internal to reduce exposure of JSON.net types so it is easier to convert
    //     to System.Text.Json
    public CosmosDbSimpleSerializer(JsonSerializerSettings jsonSerializerSettings)
    {
        SerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException("jsonSerializerSettings");
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
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            using StreamReader reader = new StreamReader(stream);
            using JsonTextReader reader2 = new JsonTextReader(reader);
            return GetSerializer().Deserialize<T>(reader2) ?? default!;
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
        MemoryStream memoryStream = new MemoryStream();
        using (StreamWriter streamWriter = new StreamWriter(memoryStream, DefaultEncoding, 1024, leaveOpen: true))
        {
            using JsonWriter jsonWriter = new JsonTextWriter(streamWriter);
            jsonWriter.Formatting = Formatting.None;
            GetSerializer().Serialize(jsonWriter, input);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        memoryStream.Position = 0L;
        return memoryStream;
    }

    //
    // Summary:
    //     JsonSerializer has hit a race conditions with custom settings that cause null
    //     reference exception. To avoid the race condition a new JsonSerializer is created
    //     for each call
    private JsonSerializer GetSerializer()
    {
        return JsonSerializer.Create(SerializerSettings);
    }
}
