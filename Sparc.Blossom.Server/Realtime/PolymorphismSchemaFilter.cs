using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Sparc.Blossom.Realtime;


public class PolymorphismSchemaFilter<T, THub> : ISchemaFilter
{
    private readonly Lazy<HashSet<Type>> derivedTypes = new(Init);

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!derivedTypes.Value.Contains(type))
            return;

        var baseProperties = typeof(T).GetProperties().Select(x => x.Name).ToList();
        var clonedProperties = schema.Properties
            .Where(x => !baseProperties.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value);

        var clonedSchema = new OpenApiSchema
        {
            Properties = clonedProperties,
            Type = schema.Type,
            Required = schema.Required
        };

        if (context.SchemaRepository.Schemas.TryGetValue(typeof(T).Name, out OpenApiSchema _))
        {
            schema.AllOf = new List<OpenApiSchema> {
            new OpenApiSchema { Reference = new OpenApiReference { Id = typeof(T).Name, Type = ReferenceType.Schema } },
            clonedSchema
        };
        }

        var assemblyName = Assembly.GetAssembly(type)!.GetName();
        schema.Discriminator = new OpenApiDiscriminator { PropertyName = "$type" };
        schema.AddExtension("x-ms-discriminator-value", new OpenApiString($"{type.FullName}, {assemblyName.Name}"));

        // reset properties for they are included in allOf, should be null but code does not handle it
        schema.Properties = new Dictionary<string, OpenApiSchema>();
    }

    private static HashSet<Type> Init()
    {
        var abstractType = typeof(T);
        var dTypes = typeof(THub).Assembly
            .GetTypes()
            .Where(x => abstractType != x && abstractType.IsAssignableFrom(x));

        var result = new HashSet<Type>();

        foreach (var item in dTypes)
            result.Add(item);

        return result;
    }
}
