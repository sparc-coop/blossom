using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        return Run<TApp, BlossomUser>(args, builderOptions, app, renderMode);
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

        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapBlossomAuthentication<TUser>();

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
        builder.Services.AddBlossomRealtime<THub>();
        
        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapHub<THub>("/_realtime");
        blossomApp.MapBlossomAuthentication<TUser>();

        app?.Invoke(blossomApp);
        blossomApp.Run();

        return blossomApp;
    }
}
