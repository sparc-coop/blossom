using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserApplicationBuilder(string[]? args = null) : IBlossomApplicationBuilder
{
    public WebAssemblyHostBuilder Builder { get; } = WebAssemblyHostBuilder.CreateDefault(args);
    public IServiceCollection Services => Builder.Services;

    public void AddAuthentication<TUser>() where TUser : BlossomUser, new()
    {
    }

    public IBlossomApplication Build()
    {
        var host = Builder.Build();
        return new BlossomBrowserApplication(host);
    }
}
