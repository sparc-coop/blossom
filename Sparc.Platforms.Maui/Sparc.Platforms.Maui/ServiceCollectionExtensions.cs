using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Sparc.Platforms.Maui;

public static class ServiceCollectionExtensions
{
    public static MauiAppBuilder Sparcify<TMainLayout>(this MauiAppBuilder builder) where TMainLayout : LayoutComponentBase
    {
        builder.RegisterBlazorMauiWebView().UseMauiApp<App>();

        builder.Services.AddBlazorWebView();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<IErrorBoundaryLogger, ConsoleErrorBoundaryLogger>()
            .AddScoped<LayoutComponentBase, TMainLayout>();

        return builder;
    }

    
}
