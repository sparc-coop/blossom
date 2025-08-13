namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcChat(
        this WebApplicationBuilder builder
    )
    {
        builder.Services
            .AddTransient<MatrixEvents>()
            .AddTransient<SparcEngineChatService>();

        return builder;
    }

    public static WebApplication UseSparcChat(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        var chatSvc = scope
            .ServiceProvider
            .GetRequiredService<SparcEngineChatService>();

        chatSvc.Map(app);
        return app;
    }
}


