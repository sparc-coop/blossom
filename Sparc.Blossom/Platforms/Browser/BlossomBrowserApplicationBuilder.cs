﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sparc.Blossom.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Authorization;
using System.Reflection;
using System.Security.Claims;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserApplicationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TApp> 
    : BlossomApplicationBuilder
    where TApp : IComponent
{
    public WebAssemblyHostBuilder Builder { get; }

    public BlossomBrowserApplicationBuilder(string[]? args = null)
    {
        Builder = WebAssemblyHostBuilder.CreateDefault(args);
        Services = Builder.Services;
        Configuration = Builder.Configuration;
    }

    public override IBlossomApplication Build()
    {
        Builder.RootComponents.Add<TApp>("#app");
        Builder.RootComponents.Add<HeadOutlet>("head::after");

        if (!_isAuthenticationAdded)
        {
            // No-config Blossom User setup
            AddAuthentication<BlossomUser>();
            Services.AddSingleton<IRepository<BlossomUser>, BlossomInMemoryRepository<BlossomUser>>();
        }

        var assembly = Assembly.GetCallingAssembly();
        RegisterBlossomEntities(assembly);
        AddBlossomRepository();
        AddBlossomRealtime(assembly);

        var host = Builder.Build();
        return new BlossomBrowserApplication<TApp>(host);
    }

    public override void AddAuthentication<TUser>()
    {
        Services.AddAuthorizationCore();
        
        Services.AddScoped<AuthenticationStateProvider, SparcEngineAuthenticationStateProvider<TUser>>()
            .AddScoped<BlossomDefaultAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<TUser>>();

        Services.AddScoped(_ => new ClaimsPrincipal(new ClaimsIdentity()));
        Services.AddScoped<BlossomUser>(s => BlossomUser.FromPrincipal(s.GetRequiredService<ClaimsPrincipal>()));    

        _isAuthenticationAdded = true;
    }
}
