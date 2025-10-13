using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

    public override IBlossomApplication Build(Assembly? entityAssembly = null)
    {
        var callingAssembly = entityAssembly ?? Assembly.GetCallingAssembly();

        Builder.RootComponents.Add<TApp>("#app");
        Builder.RootComponents.Add<HeadOutlet>("head::after");

        AddAuthentication<BlossomUser>();
        RegisterBlossomEntities(callingAssembly);
        AddBlossomRepository();

        Services.AddSingleton<TimeProvider, BrowserTimeProvider>();
        AddBlossomRealtime(callingAssembly);

        var host = Builder.Build();
        return new BlossomBrowserApplication<TApp>(host);
    }

    public override void AddAuthentication<TUser>()
    {
        Services.AddAuthorizationCore();
        Services.AddScoped<AuthenticationStateProvider, BlossomBrowserAuthenticationStateProvider<TUser>>();
        Services.AddScoped(_ => new ClaimsPrincipal(new ClaimsIdentity()));
        Services.AddScoped(s => BlossomUser.FromPrincipal(s.GetRequiredService<ClaimsPrincipal>()));

        if (!isAuthenticationAdded)
        {
            // No-config Blossom User setup
            Services.AddScoped<BlossomDefaultAuthenticator<BlossomUser>>()
                .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<BlossomUser>>();
            Services.AddScoped<IRepository<BlossomUser>, BlossomRepository<BlossomUser>>();
        }
    }
}
