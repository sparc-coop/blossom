using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Sparc.Blossom.Authentication;
using System.Reflection;

namespace Sparc.Blossom.Platforms.Windows;

public class BlossomWindowsApplicationBuilder : BlossomApplicationBuilder
{
    private readonly MauiAppBuilder MauiBuilder;

    public override IServiceCollection Services => MauiBuilder.Services;

    public BlossomWindowsApplicationBuilder(string[] args)
    {
        MauiBuilder = MauiApp.CreateBuilder();

        if (!_isAuthenticationAdded)
        {
            // No-config Blossom User setup
            AddAuthentication<BlossomUser>();
            Services.AddSingleton<IRepository<BlossomUser>, BlossomInMemoryRepository<BlossomUser>>();
        }

        MauiBuilder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        MauiBuilder.Services.AddMauiBlazorWebView();

#if DEBUG
        MauiBuilder.Services.AddBlazorWebViewDeveloperTools();
        MauiBuilder.Logging.AddDebug();
#endif

        Configuration = new ConfigurationBuilder()
            .Build();
    }

    public override void AddAuthentication<TUser>()
    {

        Services.AddScoped<BlossomDefaultAuthenticator<TUser>>()
                .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<TUser>>();

        _isAuthenticationAdded = true;
    }

    public override IBlossomApplication Build(Assembly? entityAssembly = null)
    {
        if (!_isAuthenticationAdded)
            AddAuthentication<BlossomUser>();

        var mauiApp = MauiBuilder.Build();

        return new BlossomWindowsApplication(mauiApp);
    }
}
