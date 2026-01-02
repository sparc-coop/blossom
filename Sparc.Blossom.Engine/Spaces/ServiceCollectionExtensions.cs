namespace Sparc.Blossom.Spaces;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcSpaces(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<BlossomSpaces>()
            .AddScoped<BlossomAggregateOptions<BlossomSpace>>()
            .AddScoped<BlossomAggregate<BlossomSpace>>()
            .AddTransient<BlossomVectors>();
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


