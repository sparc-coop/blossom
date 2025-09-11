using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sparc.Blossom.Authentication;
using System.Reflection;
using System.Security.Claims;

namespace Sparc.Blossom.Platforms.Android;

public class BlossomAndroidApplicationBuilder : BlossomApplicationBuilder
{
    private readonly MauiAppBuilder MauiBuilder;

    public override IServiceCollection Services => MauiBuilder.Services;

    public BlossomAndroidApplicationBuilder(string[] args)
    {
        MauiBuilder = MauiApp.CreateBuilder();

        if (!isAuthenticationAdded)
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

        if (OperatingSystem.IsAndroidVersionAtLeast(23))
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

        Services.AddScoped(_ => new ClaimsPrincipal(new ClaimsIdentity()));

        //isAuthenticationAdded = true;
    }

    public override IBlossomApplication Build(Assembly? entityAssembly = null)
    {
        var callingAssembly = Assembly.GetCallingAssembly();

        if (!isAuthenticationAdded)
        {
            AddAuthentication<BlossomUser>();
            Services.AddSingleton<IRepository<BlossomUser>, BlossomInMemoryRepository<BlossomUser>>();
        }

        RegisterBlossomEntities(callingAssembly);

        AddBlossomRepository();

        AddBlossomRealtime(callingAssembly);

        var mauiApp = MauiBuilder.Build();

        return new BlossomAndroidApplication(mauiApp);
    }
}
