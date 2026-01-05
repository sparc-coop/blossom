using Sparc.Blossom.Content;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Spaces;

public record MetricHistory(
    DateTime Date,
    double Value
);

public class BlossomSpace : BlossomEntity<string>
{
    public string Domain { get; set; }
    public string SpaceId {  get { return Id;  } set { Id = value; } }
    public string? ParentSpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BlossomSummary Summary { get; set; } = new("", "", "", null, null);
    public string RoomType { get; set; } = "Root";
    public int NumJoinedMembers { get; set; }
    public bool GuestCanJoin { get; set; }
    public bool WorldReadable { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CanonicalAlias { get; set; }
    public string? JoinRule { get; set; }
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ModelUrl { get; set; }
    public List<SparcEntityType> EntityTypes { get; set; } = [];
    public double? Consensus { get; set; }
    public double? Confidence { get; set; }
    public List<MetricHistory> ConsensusHistory { get; set; } = [];
    public List<MetricHistory> ConfidenceHistory { get; set; } = [];


    [JsonConstructor]
    protected BlossomSpace() : base(Guid.NewGuid().ToString())
    {
        Domain = string.Empty;
    }

    public BlossomSpace(string domain, string spaceId, string? roomType = null) : base(spaceId)
    {
        Domain = domain;
        SpaceId = spaceId;
        RoomType = roomType ?? "Ephemeral";
    }

    public BlossomSpace(string domain) : base(Guid.NewGuid().ToString())
    {
        Domain = domain;
        RoomType = "Ephemeral";
    }

    public BlossomSpace(BlossomSpace rootSpace, string? roomType = null)
        : this(rootSpace.Domain)
    {
        ParentSpaceId = rootSpace.SpaceId;
        RoomType = roomType ?? "Ephemeral";
    }

    public void SetSummary(BlossomSummary? summary)
    {
        if (summary == null)
            return;

        Name = summary.Name;
        Summary = summary;
    }

    public void SetConsensus(IEnumerable<BlossomPost> messages)
    {
        if (!messages.Any(x => x.IsLinked(this)))
            return;

        if (RoomType == "Facet")
        {
            Consensus = messages.Average(x => x.LinkedSpace(Id)?.Alignment ?? 0);
            Confidence = 1 / (1 + messages.Average(x => Math.Pow(x.LinkedSpace(Id)?.Alignment ?? 1, 2)));
        }
        else
        {
            Consensus = messages.Sum(x => x.LinkedSpace(Id)?.Closeness * x.LinkedSpace(Id)?.Alignment ?? 0) / messages.Sum(x => 1 - (x.LinkedSpace(Id)?.Distance ?? 1));
            // Variance is the average of the squared differences from the Mean
            Confidence = 1 / (1 + messages.Average(x => Math.Pow(x.LinkedSpace(Id)?.Distance ?? 1, 2)));
        }

        if (double.IsNaN(Consensus.Value))
            Consensus = 0;
        if (double.IsNaN(Confidence.Value))
            Confidence = 0;

        ConsensusHistory.Insert(0, new MetricHistory(DateTime.UtcNow, Consensus.Value));
        ConfidenceHistory.Insert(0, new MetricHistory(DateTime.UtcNow, Confidence.Value));
    }

    public double ConsensusDelta => ConsensusHistory.Count < 2 ? 0 :
        ConsensusHistory[0].Value - ConsensusHistory[1].Value;

    public double ConfidenceDelta => ConfidenceHistory.Count < 2 ? 0 :
        ConfidenceHistory[0].Value - ConfidenceHistory[1].Value;
}

