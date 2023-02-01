using Microsoft.Extensions.Logging;
using Sparc.Blossom.Client;
using TemplateMAUINET7.Features;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom;
using TemplateMAUINET7.UI.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Polly;
using Sparc.Blossom.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

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
#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();

            builder.Services.AddBlossomHttpClient<TemplateMAUINET7Client>("https://blossomtemplateapi.azurewebsites.net/", false);

            return builder.Build();
        }

        public static void AddBlossomHttpClient<T>(this IServiceCollection services, string? apiBaseUrl, bool configureAuthentication = true) where T : class
        {
            services.AddScoped(sp => new BlossomMauiAuthorizationMessageHandler(sp.GetRequiredService<IAccessTokenProvider>(), apiBaseUrl));

            var client = services.AddHttpClient<T>(client =>
            {
                if (apiBaseUrl != null)
                    client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestVersion = new Version(2, 0);
            })
                .AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync(new[]
                {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
                }));

            if (!configureAuthentication)
                return;

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                client.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
            else
                client.AddHttpMessageHandler<BlossomMauiAuthorizationMessageHandler>();

        }


    }

    
}