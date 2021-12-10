using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sparc.Platforms.Maui;
using $ext_safeprojectname$.Features;
using $ext_safeprojectname$.UI;
using $ext_safeprojectname$.UI.Shared;
using Microsoft.Identity.Client;
using Microsoft.Maui.Essentials;

namespace $ext_safeprojectname$.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder().Sparcify<MainLayout>();

            //TODO replace with your settings
            //more info https://sparc-coop.github.io/Sparc.Kernel/
            builder.Services.AddB2CApi<$ext_safeprojectname$Api > ( "https://api.sparctemplate.io/",
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