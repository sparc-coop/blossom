using Sparc.Blossom.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom;

public interface IBlossomApplicationBuilder
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    void AddAuthentication<TUser>() where TUser : BlossomUser, new();
    public IBlossomApplication Build();
}

public interface IBlossomApplication
{
    IServiceProvider Services { get; }
    bool IsDevelopment { get; }

    Task RunAsync<TApp>();
    Task RunAsync();
}

public class BlossomApplication
{
    public static IBlossomApplicationBuilder CreateBuilder<TApp>(string[]? args = null)
        where TApp : IComponent
    {
#if BROWSER
        return new Platforms.Browser.BlossomBrowserApplicationBuilder<TApp>(args);
#elif SERVER
        return new Platforms.Server.BlossomServerApplicationBuilder<TApp>(args ?? []);
#endif
        throw new NotImplementedException();
    }
}
