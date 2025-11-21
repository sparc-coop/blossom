namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcSpaces(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<BlossomSpaces>();
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


