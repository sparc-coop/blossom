﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Authentication;
using System.Reflection;
using System.Security.Claims;

namespace Sparc.Engine.Aura;

public static class ServiceCollectionExtensions
{
    public static void AddSparcAura(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.Name = "Sparc." + Assembly.GetEntryAssembly()?.GetName().Name;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        services.AddCascadingAuthenticationState();
        services.AddScoped<SparcAuraAuthenticator>()
            .AddScoped<IBlossomAuthenticator, SparcAuraAuthenticator>()
            .AddScoped<PasskeyAuthenticator>();

        services.AddTransient(s =>
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
