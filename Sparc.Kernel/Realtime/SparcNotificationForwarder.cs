using Microsoft.AspNetCore.SignalR;

namespace Sparc.Realtime;

public class SparcNotificationForwarder<TNotification> : RealtimeFeature<TNotification> where TNotification : SparcNotification
{
    public SparcNotificationForwarder(IHubContext hub)
    {
        Hub = hub; 
    }

    public IHubContext Hub { get; }

    public override async Task ExecuteAsync(TNotification notification)
    {
        if (notification.GroupId != null)
            await Hub.Clients.Group(notification.GroupId).SendAsync(notification.GetType().Name, notification);
        else if (notification.UserId != null)
            await Hub.Clients.User(notification.UserId).SendAsync(notification.GetType().Name, notification);
    }
}


