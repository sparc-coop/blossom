using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomHubBroadcaster<T>(IHubContext<BlossomHub> hub, IRepository<T> repository) 
    : INotificationHandler<T> 
    where T : BlossomEntityChanged
{
    public IHubContext<BlossomHub> Hub { get; } = hub;
    public IRepository<T> Repository { get; } = repository;

    public async Task ExecuteAsync(BlossomEntityChanged blossomEvent)
    {
        if (blossomEvent.SubscriptionId != null)
        {
            Console.WriteLine("Notification: " + blossomEvent.Name + " to " + blossomEvent.SubscriptionId);
            await Hub.Clients.Group(blossomEvent.SubscriptionId).SendAsync(blossomEvent.SubscriptionId, blossomEvent);
        }
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        if (notification is BlossomEntityChanged blossomEvent)
        {
            await ExecuteAsync(blossomEvent);
        }
    }
}