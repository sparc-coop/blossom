using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Sparc.Blossom.Realtime;

public class NotificationForwarder<TNotification>(IHubContext<BlossomHub> hub) : BlossomOn<TNotification> where TNotification : BlossomEvent
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
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var serializedNotification = JsonConvert.SerializeObject(notification, settings);
        return serializedNotification;
    }
}


