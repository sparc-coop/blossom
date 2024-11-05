using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Realtime;

public class NotificationForwarder<TNotification>(IHubContext<BlossomHub> hub) : BlossomOn<TNotification> where TNotification : BlossomEvent
{
    public IHubContext<BlossomHub> Hub { get; } = hub;

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.SubscriptionId != null)
        {
            Console.WriteLine("Notification: " + notification.GetType().Name + " to " + notification.SubscriptionId);
            await Hub.Clients.Group(notification.SubscriptionId).SendAsync(notification.GetType().Name, notification);
        }
    }
}


