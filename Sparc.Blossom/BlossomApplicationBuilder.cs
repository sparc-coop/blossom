﻿using Sparc.Blossom.Authentication;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MediatR.NotificationPublishers;

namespace Sparc.Blossom;

public abstract class BlossomApplicationBuilder : IBlossomApplicationBuilder
{
    public virtual IServiceCollection Services { get; protected set; } = null!;
    public virtual IConfiguration Configuration { get; protected set; } = null!;
    protected bool _isAuthenticationAdded;

    public abstract void AddAuthentication<TUser>() where TUser : BlossomUser, new();
    public abstract IBlossomApplication Build();

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

    protected void AddBlossomRepository()
    {
        if (!Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            Services.AddScoped(typeof(IRepository<>), typeof(BlossomInMemoryRepository<>));

        //Services.AddScoped(typeof(IRealtimeRepository<>), typeof(BlossomRealtimeRepository<>));
        Services.AddScoped<BlossomHubProxy>();
    }

    protected virtual void AddBlossomRealtime(Assembly assembly)
    {
        Services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(assembly);
            options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
            options.NotificationPublisher = new TaskWhenAllPublisher();
            options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });
    }


    protected static IEnumerable<Type> GetDtos(Assembly assembly)
       => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

    protected static IEnumerable<Type> GetAggregates(Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomAggregate<>));
}
