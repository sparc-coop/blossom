using Microsoft.Extensions.Logging;
using TemplateMAUINET7.MAUI.Data;
using Sparc.Blossom.Maui;

namespace TemplateMAUINET7.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.AddBlossom();
            
//            builder
//                .UseMauiApp<App>()
//                .ConfigureFonts(fonts =>
//                {
//                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
//                });

//            builder.Services.AddMauiBlazorWebView();

//#if DEBUG
//		builder.Services.AddBlazorWebViewDeveloperTools();
//		builder.Logging.AddDebug();
//#endif

//            builder.Services.AddSingleton<WeatherForecastService>();

            return builder.Build();
        }
    }
}