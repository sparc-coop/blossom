using Newtonsoft.Json;
using Sparc.Blossom;
using Sparc.Blossom.Authentication;

namespace Sparc.Core.Chat;

public class RoomMembership() : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Membership { get; set; } = string.Empty; // join/invite/leave/ban

    public string RoomId { get; set; } = string.Empty;
    public MatrixRoom? Room { get; set; }

    public string UserId { get; set; } = string.Empty;
    public BlossomUser? User { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
}


