using MediatR.NotificationPublishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;
using System.Reflection;

namespace Sparc.Blossom;

public abstract class BlossomApplicationBuilder
{
    public virtual IServiceCollection Services { get; protected set; } = null!;
    public virtual IConfiguration Configuration { get; protected set; } = null!;
    protected bool isAuthenticationAdded => Services.Any(x => x.ServiceType == typeof(IBlossomAuthenticator));

    public abstract void AddAuthentication<TUser>() where TUser : BlossomUser, new();
    public virtual void AddAuthentication<TAuthenticator, TUser>()
        where TAuthenticator : class, IBlossomAuthenticator
        where TUser : BlossomUser, new()
    { 
        AddAuthentication<TUser>();
    }

    public abstract IBlossomApplication Build(Assembly? entityAssembly = null);

    protected void RegisterBlossomEntities(Assembly assembly)
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
                typeof(IRepository<>).MakeGenericType(typeof(BlossomEntityChanged<>).MakeGenericType(entity)),
                typeof(BlossomInMemoryRepository<>).MakeGenericType(typeof(BlossomEntityChanged<>).MakeGenericType(entity)));
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

    protected void AddBlossomRepository()
    {
        Services.AddScoped(typeof(BlossomRepository<>));
        Services.AddScoped(typeof(DexieRepository<>));

        if (!Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            Services.AddScoped(typeof(IRepository<>), typeof(BlossomRepository<>));

        //Services.AddScoped(typeof(IRealtimeRepository<>), typeof(BlossomRealtimeRepository<>));
        Services.AddScoped<BlossomHubProxy>();
    }

    public abstract void AddBlossomEngine(string? url = null);
    protected void AddBlossomEngine<TTokenHandler>(string? url = null)
        where TTokenHandler : DelegatingHandler
    {
        url ??= "https://engine.sparc.coop";
        var uri = new Uri(url);

        Services.AddTransient<TTokenHandler>();

        Services.AddRefitClient<ISparcAura>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<TTokenHandler>()
            .AddStandardResilienceHandler();

        Services.AddRefitClient<ISparcBilling>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<TTokenHandler>()
            .AddStandardResilienceHandler();

        Services.AddRefitClient<ITovik>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<TTokenHandler>()
            .AddStandardResilienceHandler(x =>
            {
                x.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);
                x.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(240);
                x.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
            });

        Services.AddRefitClient<ISparcSpaces>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddHttpMessageHandler<TTokenHandler>()
            .AddStandardResilienceHandler();

        AddSparcAura();
        Services.AddScoped<SparcEvents>();
    }

    protected virtual void AddBlossomRealtime(Assembly assembly)
    {
        Services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(assembly);
            options.RegisterServicesFromAssemblyContaining<BlossomEntityChanged>();
            options.NotificationPublisher = new TaskWhenAllPublisher();
            options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });
    }

    protected abstract void AddSparcAura();

    protected static IEnumerable<Type> GetDtos(Assembly assembly)
       => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

    protected static IEnumerable<Type> GetAggregates(Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomAggregate<>));
}
