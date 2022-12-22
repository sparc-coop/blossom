using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sparc.Blossom.Realtime;

// taken from https://stackoverflow.com/questions/49006079/using-swashbuckle-for-asp-net-core-how-can-i-add-a-model-to-the-generated-model
public class PolymorphismDocumentFilter<T, THub> : IDocumentFilter
{
    public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
    {
        RegisterSubClasses(context, typeof(T));
    }

    private static void RegisterSubClasses(DocumentFilterContext context, Type abstractType)
    {
        const string discriminatorName = "$type";
        var schemaRepository = context.SchemaRepository.Schemas;
        var schemaGenerator = context.SchemaGenerator;

        if (!schemaRepository.TryGetValue(abstractType.Name, out OpenApiSchema? parentSchema))
        {
            parentSchema = schemaGenerator.GenerateSchema(abstractType, context.SchemaRepository);
        }

        // set up a discriminator property (it must be required)
        parentSchema.Discriminator = new OpenApiDiscriminator { PropertyName = discriminatorName };
        parentSchema.Required.Add(discriminatorName);

        if (!parentSchema.Properties.ContainsKey(discriminatorName))
            parentSchema.Properties.Add(discriminatorName, new OpenApiSchema { Type = "string", Default = new OpenApiString(abstractType.FullName) });

        // register all subclasses
        var derivedTypes = typeof(THub).Assembly.GetTypes()
            .Where(x => abstractType != x && abstractType.IsAssignableFrom(x));

        foreach (var type in derivedTypes)
            schemaGenerator.GenerateSchema(type, context.SchemaRepository);
    }
}
