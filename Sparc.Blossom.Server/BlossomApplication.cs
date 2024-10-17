using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom;

public static class BlossomApplication
{
    public static MauiApp Run<TApp, TLayout>() 
        where TApp : class, IApplication
        where TLayout : LayoutComponentBase
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<TApp>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<BlossomAssemblyProvider>(_ => new(typeof(TApp), typeof(TLayout)));

        return builder.Build();
    }
}
