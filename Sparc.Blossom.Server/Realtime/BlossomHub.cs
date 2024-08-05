using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Realtime;

public class BlossomHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier != null)
            await Clients.User(Context.UserIdentifier).SendAsync("_UserConnected");
    }

    public virtual async Task Watch(string subscriptionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, subscriptionId);
    }

    public virtual async Task StopWatching(string subscriptionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, subscriptionId);
    }
}
