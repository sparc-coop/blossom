using Sparc.Blossom.Content;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Spaces;

public class BlossomSpace : BlossomEntity<string>
{
    public string Domain { get; set; }
    public string SpaceId {  get { return Id;  } set { Id = value; } }
    public string? ParentSpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoomType { get; set; }
    public int NumJoinedMembers { get; set; }
    public bool GuestCanJoin { get; set; }
    public bool WorldReadable { get; set; }
    public string? Topic { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CanonicalAlias { get; set; }
    public string? JoinRule { get; set; }
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ModelUrl { get; set; }
    public List<SparcEntityType> EntityTypes { get; set; } = [];


    [JsonConstructor]
    protected BlossomSpace() : base(Guid.NewGuid().ToString())
    {
        Domain = string.Empty;
    }

    public BlossomSpace(string domain, string spaceId, string? roomType = null) : base(spaceId)
    {
        Domain = domain;
        SpaceId = spaceId;
        RoomType = roomType;
    }

    public BlossomSpace(string domain) : base(Guid.NewGuid().ToString())
    {
        Domain = domain;
        RoomType = "Ephemeral";
    }

    public BlossomSpace CreateChild() => new(Domain)
    {
        ParentSpaceId = SpaceId
    };

    public void SetSummary(BlossomSummary? summary)
    {
        if (summary == null)
            return;

        Name = summary.Name;
        Topic = summary.Topic;
        Description = summary.Description;
    }
}

