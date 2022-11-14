using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom;

namespace Sparc.Realtime;

public class SparcNotificationForwarder<TNotification> : RealtimeFeature<TNotification> where TNotification : Notification
{
    public SparcNotificationForwarder(IHubContext<SparcHub> hub)
    {
        Hub = hub; 
    }

    public IHubContext<SparcHub> Hub { get; }

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.SubscriptionId != null)
            await Hub.Clients.Group(notification.SubscriptionId).SendAsync(notification.GetType().Name, notification);
    }
}


