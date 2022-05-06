using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Sparc.Core;
using Device = Sparc.Core.Device;

namespace Sparc.Platforms.Maui;

public static class ServiceCollectionExtensions
{
    public static MauiAppBuilder Sparcify<TMainLayout>(this MauiAppBuilder builder) where TMainLayout : LayoutComponentBase
    {
        builder.UseMauiApp<App>();

        builder.Services.AddBlazorWebView();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<IErrorBoundaryLogger, ConsoleErrorBoundaryLogger>()
            .AddScoped<LayoutComponentBase, TMainLayout>()
            .AddSingleton<RootScope>();

#if ANDROID
        builder.Services.AddSingleton<Device, AndroidDevice>();
#elif IOS
        builder.Services.AddSingleton<Device, IosDevice>();
#elif MAC
        builder.Services.AddSingleton<Device, MacDevice>();
#elif WINDOWS
        builder.Services.AddSingleton<Device, WindowsDevice>();
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
