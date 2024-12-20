using MediatR;
using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class BlossomEventDefaultHandler<T>(IHubContext<BlossomHub> hub, IRepository<T> repository) 
    : INotificationHandler<T> 
    where T : BlossomEvent
{
    public IHubContext<BlossomHub> Hub { get; } = hub;
    public IRepository<T> Repository { get; } = repository;

    public async Task ExecuteAsync(BlossomEvent blossomEvent)
    {
        if (blossomEvent.SubscriptionId != null)
        {
            Console.WriteLine("Notification: " + blossomEvent.Name + " to " + blossomEvent.SubscriptionId);
            await Hub.Clients.Group(blossomEvent.SubscriptionId).SendAsync(blossomEvent.SubscriptionId, blossomEvent);
        }
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        if (notification is BlossomEvent blossomEvent)
        {
            await ExecuteAsync(blossomEvent);
        }
    }
}