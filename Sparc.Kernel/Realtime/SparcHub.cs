using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sparc.Core;
using Sparc.Kernel;

namespace Sparc.Realtime;

[Authorize]
public class SparcHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier != null)
            await Clients.User(Context.UserIdentifier).SendAsync("_UserConnected");
    }

    public virtual async Task Watch(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }

    public virtual async Task StopWatching(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }
}

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.Id();
    }
}
