using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomRealtime<TAssembly>(this IServiceCollection services)
    {
        var signalR = services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                    IgnoreSerializableInterface = false
                };
            });

        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblyContaining<TAssembly>();
            options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
            options.RegisterServicesFromAssemblyContaining<BlossomHub>();
            options.NotificationPublisher = new TaskWhenAllPublisher();
            options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });

        // Use the User ID as the SignalR user identifier    
        services.AddSingleton<IUserIdProvider, UserIdProvider>();

        return services;
    }

    public static IServiceCollection AddBlossomRealtime<TAssembly, THub>(this IServiceCollection services) where THub : BlossomHub
    {
        var signalR = services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        //.AddMessagePackProtocol();

        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblyContaining<TAssembly>();
            options.RegisterServicesFromAssemblyContaining<BlossomEvent>();
            options.RegisterServicesFromAssemblyContaining<THub>();
            options.NotificationPublisher = new TaskWhenAllPublisher();
            options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });

        // Use the User ID as the SignalR user identifier    
        services.AddSingleton<IUserIdProvider, UserIdProvider>();
        return services;
    }
}
