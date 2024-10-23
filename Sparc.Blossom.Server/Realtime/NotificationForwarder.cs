using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Core.Serialization;
using Sparc.Blossom.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public class NotificationForwarder<TNotification>(IHubContext<BlossomHub> hub) : BlossomOn<TNotification> 
    where TNotification : BlossomEvent
{
    public IHubContext<BlossomHub> Hub { get; } = hub;

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.SubscriptionId != null)
        {
            Console.WriteLine("Notification: " + notification.GetType().Name + " to " + notification.SubscriptionId);
            var methodName = notification.GetType().Name;

            string serializedNotification = SerializeNotification(notification);

            await Hub.Clients.Group(notification.SubscriptionId).SendAsync(methodName, serializedNotification);
        }
    }

    private static string SerializeNotification(TNotification notification)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true
        };

        options.Converters.Add(new PolymorphicJsonConverter<BlossomEntity>());

        var serializedNotification = JsonSerializer.Serialize(notification, notification.GetType(), options);
        return serializedNotification;
    }
}


