using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sparc.Platforms.Maui;
using SparcTemplate.Features;
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

            //TODO replace with your settings
            //more info https://sparc-coop.github.io/Sparc.Kernel/
            builder.Services.AddB2CApi<SparcTemplateApi>( "https://api.sparctemplate.io/",
                    new("sparctemplate",
                    "",
                    "SparcTemplate.API",
                    parentWindowLocator: () =>
                    {
#if ANDROID
                        return Platform.CurrentActivity;
#else
                        return null;
#endif
                    }));



            return builder.Build();
        }
    }
}