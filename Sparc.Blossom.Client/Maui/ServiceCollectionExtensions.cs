using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Sparc.Blossom;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Realtime;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Polly;
//using Device = Sparc.Blossom.Authentication.Device;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static MauiAppBuilder AddBlossom<TMainLayout>(this MauiAppBuilder builder) where TMainLayout : LayoutComponentBase
    {
        //builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        
        builder.Services.AddScoped<IErrorBoundaryLogger, ConsoleErrorBoundaryLogger>()
            .AddScoped<LayoutComponentBase, TMainLayout>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
        //builder.Logging.AddDebug();
#endif
        //var configuration = builder.Configuration;
        //builder.Services.AddScoped(_ => configuration);
        //var hasAuth = configuration["AzureAdB2C:Authority"] != null || configuration["Blossom:Authority"] != null;

        //if (configuration["AzureAdB2C:Authority"] != null)
        //    builder.Services.AddB2CApi<T>(configuration);
        //if (configuration["Oidc:Authority"] != null)
        //    services.AddOidcApi<T>(configuration);
        //if (configuration["Blossom:Authority"] != null)
        //    services.AddBlossomApi<T>();

        //if (!hasAuth)
        //{
        //    services.AddAuthorizationCore();
        //    services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
        //}

        //builder.Services.AddBlossomHttpClient<T>(baseUrl, hasAuth);

        //#if ANDROID
        //        builder.Services.AddSingleton<Device, AndroidDevice>();
        //#elif IOS
        //        builder.Services.AddSingleton<Device, IosDevice>();
        //#elif MAC
        //        builder.Services.AddSingleton<Device, MacDevice>();
        //#elif WINDOWS
        //        builder.Services.AddSingleton<Device, WindowsDevice>();
        //#else
        //        builder.Services.AddSingleton<Device, WebDevice>();
        //#endif

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

    public static Task<IServiceCollection> AddB2CApi<T>(this IServiceCollection services, string baseUrl, AzureADB2CSettings b2CSettings) where T : class
    {
        services.AddAuthorizationCore();
        services.AddScoped(_ => b2CSettings);
        services.AddSingleton<AzureADB2CAuthenticator>();
        services.AddSingleton<AuthenticationStateProvider>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddSingleton<IAuthenticator>(s => s.GetRequiredService<AzureADB2CAuthenticator>());
        services.AddScoped<AzureADB2CAuthorizationMessageHandler>();

        services.AddHttpClient("api")
            .AddHttpMessageHandler<AzureADB2CAuthorizationMessageHandler>();

        services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), baseUrl, x.GetService<IHttpClientFactory>().CreateClient("api")));

        return Task.FromResult(services);
    }

    
}
