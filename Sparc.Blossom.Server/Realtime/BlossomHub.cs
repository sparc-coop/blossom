using Microsoft.AspNetCore.SignalR;

namespace Sparc.Blossom.Realtime;

public class BlossomHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier != null)
        {
            Console.WriteLine($"OnConnectedAsync {Context.UserIdentifier}");
            await Clients.User(Context.UserIdentifier).SendAsync("_UserConnected");
        }
    }

    public virtual async Task Watch(List<string> subscriptionIds)
    {
        foreach (var subscriptionId in subscriptionIds)
        {
            Console.WriteLine($"{Context.UserIdentifier} is watching {subscriptionId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, subscriptionId);
        }
    }

    public virtual async Task StopWatching(List<string> subscriptionIds)
    {
        foreach (var subscriptionId in subscriptionIds)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, subscriptionId);
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        Console.WriteLine($"Disconnected {e?.Message} {Context.ConnectionId}");
        await base.OnDisconnectedAsync(e);
    }

    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync(groupName, message);
    }
}
