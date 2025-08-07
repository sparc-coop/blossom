using Sparc.Blossom;

namespace Sparc.Core.Chat;

public class MatrixRoom(string roomName) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string RoomId { get { return Id; } set { Id = value; } } // Partition key
    public string RoomName { get; set; } = roomName;
    public string CreatorUserId { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }

    public ICollection<RoomMembership> Memberships { get; set; } = new List<RoomMembership>();
}

public record PreviousRoom(string RoomId, string EventId);
public record CreateRoom(string RoomVersion = "1", bool Federate = true, string? Type = null, PreviousRoom? PreviousRoom = null);