using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Realtime;

public class NotificationForwarder<TNotification>(IHubContext<BlossomHub> hub) : RealtimeFeature<TNotification> where TNotification : Notification
{
    public IHubContext<BlossomHub> Hub { get; } = hub;

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.SubscriptionId != null)
            await Hub.Clients.Group(notification.SubscriptionId).SendAsync(notification.GetType().Name, notification);
    }
}


