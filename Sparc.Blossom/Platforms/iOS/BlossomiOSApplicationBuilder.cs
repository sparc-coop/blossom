using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.iOS;

public class BlossomiOSApplicationBuilder : BlossomApplicationBuilder
{
    private readonly MauiAppBuilder MauiBuilder;

    public override IServiceCollection Services => MauiBuilder.Services;

    public BlossomiOSApplicationBuilder(string[] args)
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

    public override IBlossomApplication Build()
    {

        if (!_isAuthenticationAdded)
        {
            AddAuthentication<BlossomUser>();
        }


        var mauiApp = MauiBuilder.Build();

        return new BlossomiOSApplication(mauiApp);
    }
}
