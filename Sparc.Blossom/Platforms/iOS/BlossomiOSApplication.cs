namespace Sparc.Blossom.Platforms.iOS;

public class BlossomiOSApplication : IBlossomApplication
{
    public static MauiApp MauiApp = null!;

    public BlossomiOSApplication(MauiApp mauiApp)
    {
        MauiApp = mauiApp;
    }

    public IServiceProvider Services => MauiApp.Services;

    public bool IsDevelopment
    {
        get { return false; }
    }

    public Task RunAsync()
    {
        return Task.CompletedTask;
    }

    public void Run()
    {

    }

    public Task RunAsync<TApp>()
    {
        return RunAsync();
    }
}
