using Microsoft.AspNetCore.SignalR;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.Server;

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.Id();
    }
}
