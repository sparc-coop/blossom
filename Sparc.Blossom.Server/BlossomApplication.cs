using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;
using System.Reflection;

namespace Sparc.Blossom;

public static class BlossomApplication
{
    public static WebApplication Run<TApp>(
        string[] args,
        Action<WebApplicationBuilder>? builderOptions = null,
        Action<WebApplication>? app = null,
        IComponentRenderMode? renderMode = null,
        Assembly? apiAssembly = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddBlossom<BlossomUser>(builderOptions, renderMode, apiAssembly);
        builder.Services.AddSingleton<IRepository<BlossomUser>, BlossomInMemoryRepository<BlossomUser>>();
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
}
