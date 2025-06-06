﻿using Sparc.Blossom.Authentication;
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
    void Run();
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

    public static IBlossomApplicationBuilder CreateBuilder(string[]? args = null)
    {
#if ANDROID
        return new Platforms.Android.BlossomAndroidApplicationBuilder(args ?? Array.Empty<string>());
#elif IOS
        return new Platforms.iOS.BlossomiOSApplicationBuilder(args ?? Array.Empty<string>());
#elif WINDOWS
        return new Platforms.Windows.BlossomWindowsApplicationBuilder(args ?? Array.Empty<string>());
#endif
        throw new NotImplementedException();
    }
}


