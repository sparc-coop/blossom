using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Platforms.Server;
using System.Security.Claims;

namespace Sparc.Engine;

public static class ServiceCollectionExtensions
{
    public static void AddSparcEngine(this IServiceCollection services, Uri? uri = null)
    {
        uri ??= new Uri("https://engine.sparc.coop");

        services.AddHttpContextAccessor();
        services.AddTransient<SparcCookieHandler>();
        services.AddRefitClient<ISparcEngine>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<SparcCookieHandler>()
            .AddStandardResilienceHandler();

        services.AddSparcEngineAuthentication();
    }

    public static void AddSparcEngineAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<BlossomUser>>()
            .AddScoped<SparcEngineAuthenticator>()
            .AddScoped<IBlossomAuthenticator, SparcEngineAuthenticator>();

        services.AddTransient(s =>
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
