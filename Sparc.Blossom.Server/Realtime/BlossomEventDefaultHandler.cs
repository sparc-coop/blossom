using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class BlossomEventDefaultHandler<T>(IHubContext<BlossomHub> hub, IRepository<T> repository) 
    : BlossomOn<BlossomEvent<T>> 
    where T : BlossomEntity
{
    public IHubContext<BlossomHub> Hub { get; } = hub;
    public IRepository<T> Repository { get; } = repository;

    public override async Task ExecuteAsync(BlossomEvent<T> blossomEvent)
    {
        if (blossomEvent.SubscriptionId != null)
        {
            Console.WriteLine("Notification: " + blossomEvent.Name + " to " + blossomEvent.SubscriptionId);
            await Hub.Clients.Group(blossomEvent.SubscriptionId).SendAsync(blossomEvent.SubscriptionId, blossomEvent);
        }

        if (blossomEvent is BlossomEntityDeleted<T>)
        {
            await Repository.DeleteAsync(blossomEvent.Entity);
        }
        else
        {
            await Repository.UpdateAsync(blossomEvent.Entity);
        }
    }
}