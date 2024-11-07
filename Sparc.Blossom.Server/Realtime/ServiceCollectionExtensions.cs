using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomRealtime<TAssembly>(this IServiceCollection services)
    {
        return services.AddBlossomRealtime<TAssembly, BlossomHub>();
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
