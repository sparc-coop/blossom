using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Realtime;

public class NotificationForwarder<TNotification> : RealtimeFeature<TNotification> where TNotification : Notification
{
    public NotificationForwarder(IHubContext<BlossomHub> hub)
    {
        Hub = hub; 
    }

    public IHubContext<BlossomHub> Hub { get; }

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.SubscriptionId != null)
            await Hub.Clients.Group(notification.SubscriptionId).SendAsync(notification.GetType().Name, notification);
    }
}


