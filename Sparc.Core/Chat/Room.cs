using Sparc.Blossom;

namespace Sparc.Core.Chat;

public class Room(string roomName) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string RoomId { get { return Id; } set { Id = value; } } // Partition key
    public string RoomName { get; set; } = roomName;
    public string Topic { get; set; } = string.Empty;
    public string CreatorUserId { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }

    public ICollection<RoomMembership> Memberships { get; set; } = new List<RoomMembership>();
}


