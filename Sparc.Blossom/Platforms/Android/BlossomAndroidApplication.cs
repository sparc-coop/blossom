namespace Sparc.Blossom.Platforms.Android;

public class BlossomAndroidApplication : IBlossomApplication
{
    public static MauiApp? MauiApp;

    public BlossomAndroidApplication(MauiApp mauiApp)
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