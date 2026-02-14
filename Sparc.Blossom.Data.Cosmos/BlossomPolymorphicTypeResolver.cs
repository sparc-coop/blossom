using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Sparc.Blossom.Data;

public class BlossomPolymorphicTypeResolver : DefaultJsonTypeInfoResolver
{
    static Dictionary<Type, List<JsonDerivedType>>? DerivedTypes;
    static Dictionary<Type, JsonTypeInfo> TypeInfoCache = [];

    static BlossomPolymorphicTypeResolver()
    {
        if (DerivedTypes is null)
        {
            DerivedTypes = [];
            // Search all assemblies for derived types
            var entities = AppDomain.CurrentDomain.GetDerivedTypes(typeof(BlossomEntity<string>));
            foreach (var entity in entities)
            {
                var derivedTypes = AppDomain.CurrentDomain.GetDerivedTypes(entity);
                if (derivedTypes.Any())
                    DerivedTypes.Add(entity, derivedTypes.Where(x => !x.IsAbstract).Select(x => new JsonDerivedType(x, x.Name)).ToList());
            }
        }
    }
    
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (TypeInfoCache.ContainsKey(type))
            return TypeInfoCache[type];
        
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);
        if (DerivedTypes?.Any() == true && DerivedTypes.ContainsKey(jsonTypeInfo.Type))
        {
            jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                IgnoreUnrecognizedTypeDiscriminators = true,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType,
            };

            foreach (var derivedType in DerivedTypes[jsonTypeInfo.Type])
                jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(derivedType);

        }

        TypeInfoCache.TryAdd(type, jsonTypeInfo);

        return jsonTypeInfo;
    }
}