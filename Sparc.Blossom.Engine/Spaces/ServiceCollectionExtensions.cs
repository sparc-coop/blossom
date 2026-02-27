using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace Sparc.Blossom.Spaces;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcSpaces(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<BlossomSpaces>()
            .AddScoped<BlossomAggregateOptions<BlossomSpace>>()
            .AddScoped<BlossomAggregate<BlossomSpace>>()
            .AddTransient<BlossomSpaceFacets>()
            .AddTransient<BlossomSpaceConstellations>()
            .AddTransient<BlossomPosts>()
            .AddTransient<BlossomSpaceTranslator>()
            .AddTransient<BlossomSpaceObjects>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(BlossomSpaceObject.DoNotSerializeVectors);
        });
        
        return builder;
    }

    public static WebApplication UseSparcSpaces(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var spaces = scope.ServiceProvider.GetRequiredService<BlossomSpaces>();
        spaces.Map(app);
        return app;
    }
}


