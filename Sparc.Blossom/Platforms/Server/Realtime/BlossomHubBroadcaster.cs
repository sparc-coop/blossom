using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomHubBroadcaster<T>(IHubContext<BlossomHub> hub) : BlossomOn<BlossomEvent>
{
    public override async Task ExecuteAsync(BlossomEvent ev)
    {
        Console.WriteLine($"Notification: {ev.SubscriptionId}");
        await hub.Clients.Group(ev.SubscriptionId).SendAsync(ev.SubscriptionId, ev);
    }
}