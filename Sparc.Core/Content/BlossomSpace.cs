using System.Text.Json.Serialization;

namespace Sparc.Blossom;

public class BlossomSpace : BlossomEntity<string>
{
    public string Domain { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomId { get; set; }
    public string? RoomType { get; set; }
    public int NumJoinedMembers { get; set; }
    public bool GuestCanJoin { get; set; }
    public bool WorldReadable { get; set; }
    public string? Topic { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CanonicalAlias { get; set; }
    public string? JoinRule { get; set; }
    public string LocalId => RoomId.Split(':').First();
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }


    [JsonConstructor]
    protected BlossomSpace() : base("")
    {
        Domain = string.Empty;
        RoomId = string.Empty;
    }

    public BlossomSpace(string domain, string roomId, string? roomType = null) : base(roomId)
    {
        Domain = domain;
        RoomId = roomId;
        RoomType = roomType;
    }
}

