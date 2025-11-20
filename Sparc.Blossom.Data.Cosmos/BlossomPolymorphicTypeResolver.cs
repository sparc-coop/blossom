using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Sparc.Blossom.Data;

public class BlossomPolymorphicTypeResolver<T> : DefaultJsonTypeInfoResolver
{
    static readonly Type BaseType = typeof(T);
    static readonly List<JsonDerivedType>? DerivedTypes;

    static BlossomPolymorphicTypeResolver()
    {
        if (DerivedTypes is null)
        {
            DerivedTypes = [];
            // Search all assemblies for derived types
            var derivedTypes = AppDomain.CurrentDomain.GetDerivedTypes(BaseType);
            foreach (var derivedType in derivedTypes.Where(x => !x.IsAbstract))
                DerivedTypes.Add(new JsonDerivedType(derivedType, derivedType.Name));
        }
    }
    
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);
        if (DerivedTypes?.Any() == true && jsonTypeInfo.Type == BaseType)
        {
            jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                IgnoreUnrecognizedTypeDiscriminators = true,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType,
            };

            foreach (var derivedType in DerivedTypes)
                jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(derivedType);
        }

        return jsonTypeInfo;
    }
}