namespace Sparc.Blossom.Platforms.Windows;

public class BlossomWindowsApplication : IBlossomApplication
{
    public static MauiApp? MauiApp;

    public BlossomWindowsApplication(MauiApp mauiApp)
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
