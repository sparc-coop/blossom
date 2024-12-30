using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserApplication(WebAssemblyHost host) : IBlossomApplication
{
    public WebAssemblyHost Host { get; set; } = host;

    public async Task RunAsync<TApp>()
    {
        await Host.RunAsync();
    }
}
