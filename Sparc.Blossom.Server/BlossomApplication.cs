using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public static class BlossomApplication
{
    public static WebApplication Run<TApp, TUser, THub>(
        string[] args, 
        Action<IServiceCollection, IConfiguration>? services = null,
        Action<WebApplication>? app = null,
        IComponentRenderMode? renderMode = null)
        where TUser : BlossomUser, new()
        where THub : BlossomHub
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddBlossom<TUser>(services, renderMode);
        builder.Services.AddBlossomRealtime<THub>();
        
        var blossomApp = builder.UseBlossom<TApp>();
        blossomApp.MapHub<THub>("/_realtime");
        blossomApp.MapIdentityApi<TUser>();

        app?.Invoke(blossomApp);
        blossomApp.Run();

        return blossomApp;
    }
}
