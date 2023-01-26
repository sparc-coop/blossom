using Microsoft.Extensions.Logging;
using TemplateMAUINET7.MAUI.Data;
using Sparc.Blossom.Client;
using TemplateMAUINET7.Features;
using TemplateMAUINET7.MAUI.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom;
using TemplateMAUINET7.UI.Shared;

namespace TemplateMAUINET7.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();
            builder.AddBlossom<MainLayout>();
            
            //builder.Services.AddSingleton<WeatherForecastService>();

            return builder.Build();
        }

        
    }

    
}