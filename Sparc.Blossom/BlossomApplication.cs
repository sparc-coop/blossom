using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom;

public interface IBlossomApplication
{
    IServiceProvider Services { get; }
    bool IsDevelopment { get; }

    Task RunAsync<TApp>();
}

public class BlossomApplication
{
    public static BlossomApplicationBuilder CreateBuilder<TApp>(string[]? args = null)
        where TApp : IComponent
    {
#if BROWSER
        return new Platforms.Browser.BlossomBrowserApplicationBuilder<BlossomApp<TApp>>(args);
#elif SERVER
        return new Platforms.Server.BlossomServerApplicationBuilder(args ?? []);
#elif ANDROID
        return new Platforms.Android.BlossomAndroidApplicationBuilder(args ?? Array.Empty<string>());
#elif IOS
        return new Platforms.iOS.BlossomiOSApplicationBuilder(args ?? Array.Empty<string>());
#elif WINDOWS
        return new Platforms.Windows.BlossomWindowsApplicationBuilder(args ?? Array.Empty<string>());
#endif
        throw new NotImplementedException();
    }
}


