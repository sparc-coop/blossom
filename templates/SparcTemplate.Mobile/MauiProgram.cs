using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sparc.Platforms.Maui;
using SparcTemplate.Features;
using SparcTemplate.Mobile.Data;
using SparcTemplate.UI;
using SparcTemplate.UI.Shared;
using Microsoft.Identity.Client;
using Microsoft.Maui.Essentials;

namespace SparcTemplate.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder().Sparcify<MainLayout>();


            //builder
            //    .RegisterBlazorMauiWebView()
            //    .UseMauiApp<App>()
            //    .ConfigureFonts(fonts =>
            //    {
            //        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            //    });

            //builder.Services.AddBlazorWebView();

            builder.Services.AddB2CApi<SparcTemplateApi>( "https://api.prontoservices.io/",
                    new("prontopay",
                    "",
                    "ProntoPay.API",
                    parentWindowLocator: () =>
                    {
#if ANDROID
                        return Platform.CurrentActivity;
#else
                        return null;
#endif
                    }));


            builder.Services.AddSingleton<WeatherForecastService>();

            return builder.Build();
        }
    }
}