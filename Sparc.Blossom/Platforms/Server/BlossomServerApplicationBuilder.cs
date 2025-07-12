using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Authentication;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomServerApplicationBuilder<TApp> : BlossomApplicationBuilder
{
    public WebApplicationBuilder Builder { get; }

    public BlossomServerApplicationBuilder(string[] args)
    {
        Builder = WebApplication.CreateBuilder(args);
        Services = Builder.Services;
        Configuration = Builder.Configuration;
    }

    public override IBlossomApplication Build(Assembly? entityAssembly = null)
    {
        var callingAssembly = entityAssembly ?? Assembly.GetCallingAssembly();

        if (!isAuthenticationAdded)
        {
            // No-config Blossom User setup
            AddAuthentication<BlossomUser>();
            Services.AddSingleton<IRepository<BlossomUser>, BlossomInMemoryRepository<BlossomUser>>();
        }

        AddBlossomServer();
        RegisterBlossomEntities(callingAssembly);

        if (Builder.Environment.IsDevelopment())
            Services.AddEndpointsApiExplorer();

        AddBlossomRepository();

        Services.AddScoped<TimeProvider, BrowserTimeProvider>();

        Services.AddServerSideBlazor();
        Services.AddHttpContextAccessor();
        Services.AddOutputCache();

        AddBlossomRealtime(callingAssembly);

        return new BlossomServerApplication(Builder);
    }

    public override void AddAuthentication<TUser>()
    {
        Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        Services.AddCascadingAuthenticationState();
        Services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<TUser>>();

        Services.AddTransient(s => 
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User 
            ?? new ClaimsPrincipal(new ClaimsIdentity()));

        Services.AddTransient(s => BlossomUser.FromPrincipal(s.GetRequiredService<ClaimsPrincipal>()));
    }

    public override void AddAuthentication<TAuthenticator, TUser>()
    {
        Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.SameSite = SameSiteMode.None;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        Services.AddCascadingAuthenticationState();
        Services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<TUser>>()
            .AddScoped<TAuthenticator>()
            .AddScoped<IBlossomAuthenticator, TAuthenticator>();

        Services.AddTransient(s =>
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity()));
    }

    void AddBlossomServer(IComponentRenderMode? renderMode = null)
    {
        var razor = Services.AddRazorComponents();
        renderMode ??= RenderMode.InteractiveAuto;

        if (renderMode is InteractiveServerRenderMode || renderMode is InteractiveAutoRenderMode)
            razor.AddInteractiveServerComponents();

        if (renderMode is InteractiveWebAssemblyRenderMode || renderMode is InteractiveAutoRenderMode)
            razor.AddInteractiveWebAssemblyComponents();
    }

    protected override void AddBlossomRealtime(Assembly assembly) => AddBlossomRealtime<BlossomHub>(assembly);

    void AddBlossomRealtime<THub>(Assembly assembly)
    { 
        var signalR = Services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        //.AddMessagePackProtocol();

        Services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(assembly);
            options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
            options.RegisterServicesFromAssemblyContaining<BlossomHub>();
            options.RegisterServicesFromAssemblyContaining<THub>();
            options.NotificationPublisher = new TaskWhenAllPublisher();
            options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });

        // Use the User ID as the SignalR user identifier    
        Services.AddSingleton<IUserIdProvider, UserIdProvider>();
    }
}
