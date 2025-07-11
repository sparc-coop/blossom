﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserApplication<TLayout>(WebAssemblyHost host) : IBlossomApplication
{
    public WebAssemblyHost Host { get; set; } = host;
    public IServiceProvider Services => Host.Services;
    public bool IsDevelopment => false;

    public async Task RunAsync<TApp>()
    {
        await Host.RunAsync();
    }
}
