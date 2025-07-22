namespace Sparc.Core.Chat;

public class RoomMembership
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Membership { get; set; } = string.Empty; // join/invite/leave/ban
}


