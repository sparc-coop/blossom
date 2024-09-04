using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public static class BlossomApplication
{
    public static WebApplication Run<TApp>(
        string[] args,
        Action<WebApplicationBuilder>? builderOptions = null,
        Action<WebApplication>? app = null,
        IComponentRenderMode? renderMode = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddBlossom(builderOptions, renderMode);
        builder.Services.AddBlossomRealtime<TApp>();

        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapHub<BlossomHub>("/_realtime");

        app?.Invoke(blossomApp);
        blossomApp.Run();

        return blossomApp;
    }

    public static WebApplication Run<TApp, TUser>(
        string[] args,
        Action<WebApplicationBuilder>? builderOptions = null,
        Action<WebApplication>? app = null,
        IComponentRenderMode? renderMode = null)
        where TUser : BlossomUser, new()
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddBlossom<TUser>(builderOptions, renderMode);
        builder.Services.AddBlossomRealtime<TApp>();

        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapHub<BlossomHub>("/_realtime");

        app?.Invoke(blossomApp);
        blossomApp.Run();

        return blossomApp;
    }

    public static WebApplication Run<TApp, TUser, THub>(
        string[] args, 
        Action<WebApplicationBuilder>? builderOptions = null,
        Action<WebApplication>? app = null,
        IComponentRenderMode? renderMode = null)
        where TUser : BlossomUser, new()
        where THub : BlossomHub
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddBlossom<TUser>(builderOptions, renderMode);
        builder.Services.AddBlossomRealtime<TApp, THub>();
        
        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapHub<THub>("/_realtime");

        app?.Invoke(blossomApp);
        blossomApp.Run();

        return blossomApp;
    }

    public static MauiApp CreateMauiApp<TApp>(Action<MauiAppBuilder>? builderOptions = null, Action<MauiApp>? app = null) where TApp : class, IApplication
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<TApp>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Register services
        //builder.Services.AddBlossomRealtime<TApp>();

        return builder.Build();
    }
}
