﻿using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomRealtime<THub>(this IServiceCollection services) where THub : BlossomHub
    {
        var signalR = services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        //.AddMessagePackProtocol();

        services.AddMediatR(typeof(THub).Assembly, typeof(Notification).Assembly, typeof(NotificationForwarder<>).Assembly);
        services.AddSingleton<BlossomNotifier>();

        // Use the User ID as the SignalR user identifier    
        services.AddSingleton<IUserIdProvider, UserIdProvider>();

        services.AddTransient<IHubContext<BlossomHub>>(s => s.GetRequiredService<IHubContext<THub>>());

        return services;
    }
}
