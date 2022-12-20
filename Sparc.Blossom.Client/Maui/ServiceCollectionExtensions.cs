using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static MauiAppBuilder AddBlossom<TApp, TMainLayout>(this MauiAppBuilder builder)
        where TApp : Application
        where TMainLayout : LayoutComponentBase
    {
        builder.UseMauiApp<TApp>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddAuthorizationCore();
        builder.Services
            .AddScoped<IErrorBoundaryLogger, ConsoleErrorBoundaryLogger>()
            .AddScoped<LayoutComponentBase, TMainLayout>();

#if ANDROID
        builder.Services.AddSingleton<Device, AndroidDevice>();
#elif IOS
        builder.Services.AddSingleton<Device, IosDevice>();
#elif MAC
        builder.Services.AddSingleton<Device, MacDevice>();
#elif WINDOWS
        builder.Services.AddSingleton<IDevice, WindowsDevice>();
#else
        builder.Services.AddSingleton<Device, WebDevice>();
#endif

        return builder;
    }

    public static MauiAppBuilder AddPushNotifications(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.Services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
#elif IOS
        builder.Services.AddSingleton<IPushNotificationService, IosPushNotificationService>();
#elif MAC
        builder.Services.AddSingleton<IPushNotificationService, MacPushNotificationService>();
#elif WINDOWS
        builder.Services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
#endif
        return builder;
    }


}
