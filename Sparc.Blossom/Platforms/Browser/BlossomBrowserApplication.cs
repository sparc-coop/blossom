using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserApplication(WebAssemblyHost host) : IBlossomApplication
{
    public WebAssemblyHost Host { get; set; } = host;
    public IServiceProvider Services => Host.Services;

    public async Task RunAsync()
    {
        await Host.RunAsync();
    }

    public async Task RunAsync<TApp>()
    {
        await RunAsync();
    }
}
