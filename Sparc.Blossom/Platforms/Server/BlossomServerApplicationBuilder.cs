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

public class BlossomServerApplicationBuilder(string[] args) : IBlossomApplicationBuilder
{
    public WebApplicationBuilder Builder { get; } = WebApplication.CreateBuilder(args);
    public IServiceCollection Services => Builder.Services;
    bool _isAuthenticationAdded;

    public IBlossomApplication Build()
    {
        var callingAssembly = Assembly.GetCallingAssembly();

        if (!_isAuthenticationAdded)
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

        Services.AddServerSideBlazor();
        Services.AddHttpContextAccessor();
        Services.AddOutputCache();

        AddBlossomRealtime(callingAssembly);

        return new BlossomServerApplication(Builder);
    }

    public void AddAuthentication<TUser>() where TUser : BlossomUser, new()
    {
        Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromDays(30));

        Services.AddCascadingAuthenticationState();
        Services.AddScoped<AuthenticationStateProvider, BlossomServerAuthenticationStateProvider<TUser>>()
            .AddScoped<BlossomDefaultAuthenticator<TUser>>()
            .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<TUser>>();

        Services.AddTransient(s => 
            s.GetRequiredService<IHttpContextAccessor>().HttpContext?.User 
            ?? new ClaimsPrincipal(new ClaimsIdentity()));

        _isAuthenticationAdded = true;
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

    void RegisterBlossomEntities(Assembly assembly)
    {
        Services.AddScoped(typeof(BlossomAggregateProxy<>));

        var apis = assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>));
        foreach (var api in apis)
            Services.AddScoped(api);

        var aggregates = GetAggregates(assembly);
        Services.AddScoped(typeof(BlossomAggregateOptions<>));
        Services.AddScoped(typeof(BlossomAggregate<>));

        var entities = assembly.GetEntities();
        foreach (var entity in entities)
        {
            Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(entity),
                typeof(BlossomAggregate<>).MakeGenericType(entity));

            Services.AddScoped(
                typeof(IRepository<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)),
                typeof(BlossomInMemoryRepository<>).MakeGenericType(typeof(BlossomEvent<>).MakeGenericType(entity)));
        }

        foreach (var aggregate in aggregates)
        {
            var baseOfType = aggregate.BaseType!.GenericTypeArguments.First();
            Services.AddScoped(typeof(BlossomAggregate<>).MakeGenericType(baseOfType), aggregate);
            Services.AddScoped(typeof(IRunner<>).MakeGenericType(baseOfType), aggregate);
            Services.AddScoped(aggregate);
        }

        var dtos = GetDtos(assembly)
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null);

        foreach (var dto in dtos)
            Services.AddScoped(
                typeof(IRunner<>).MakeGenericType(dto.Key),
                typeof(BlossomProxyRunner<,>).MakeGenericType(dto.Key, dto.Value!));

        foreach (var api in assembly.GetTypes<IBlossomApi>())
            Services.AddScoped(api);
    }

    void AddBlossomRepository()
    {
        if (!Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            Services.AddScoped(typeof(IRepository<>), typeof(BlossomInMemoryRepository<>));

        Services.AddScoped(typeof(IEventRepository<>), typeof(BlossomEventRepository<>));
        Services.AddScoped<BlossomSignalRProxy>();
    }

    void AddBlossomRealtime(Assembly assembly) => AddBlossomRealtime<BlossomHub>(assembly);

    void AddBlossomRealtime<THub>(Assembly assembly) where THub : BlossomHub
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

    static IEnumerable<Type> GetDtos(Assembly assembly)
       => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

    static IEnumerable<Type> GetAggregates(Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomAggregate<>));
}
