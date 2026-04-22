using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomHubBroadcaster<T>(IHubContext<BlossomHub> hub) : BlossomOn<BlossomEvent>
{
    public override async Task ExecuteAsync(BlossomEvent ev)
    {
        Console.WriteLine($"Notification: {ev.Source}");
        await hub.Clients.Group(ev.Source).SendAsync(ev.Source, ev);
    }
}