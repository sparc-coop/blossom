using Sparc.Blossom.Content;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Spaces;

public record MetricHistory(
    DateTime Date,
    double Value
);

public class BlossomSpaceSettings
{
    public float HeadspaceVelocity { get; set; } = 5.0f;
    public float SpaceGravity { get; set; } = 1.0f;
    public int MessageLookback { get; set; } = 0;
    public float UserHeadspaceWeight { get; set; } = 0.02f;
    public float MessageLookbackWeight { get; set; } = 0.02f;
    public int ConstellationStrength { get; set; } = 5;
    public float ConstellationThreshold { get; set; } = 0.2f;
}
    
public class BlossomSpace : BlossomSpaceObject
{
    public string Name { get; set; } = string.Empty;
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
    public double? Weight { get; set; }
    public float Coherence { get; set; }
    public BlossomSpaceSettings Settings { get; set; } = new();
    public List<Axis> Axes { get; set; } = [];
    public BlossomVector Origin { get; set; } = new();

    public string? ActiveQuestId { get; set; }

    [JsonConstructor]
    protected BlossomSpace() : base(Guid.NewGuid().ToString())
    {
        Id = SpaceId;
    }

    public BlossomSpace(string id, string? roomType = null) : base(id)
    {
        Id = id;
        RoomType = roomType ?? "Ephemeral";
    }

    public BlossomSpace(BlossomSpace parentSpace, string? roomType = null)
        : this()
    {
        SpaceId = parentSpace.Id;
        RoomType = roomType ?? "Ephemeral";
    }

    public override void SetSummary(BlossomSummary? summary)
    {
        base.SetSummary(summary);

        if (summary != null)
            Name = summary.Name;
    }

    public override void MaterializeCoordinates(List<Axis> axes)
    {
        var coordinateVector = RoomType == "User" ? Origin : Vector;
        base.MaterializeCoordinates(axes, coordinateVector);
    }

    public void Add(Post post, Post? previousPost, BlossomSpace alignmentSpace)
    {
        var semanticChange = previousPost == null ? 1 : post.Vector.Subtract(previousPost.Vector).Magnitude();
        var confidence = alignmentSpace.Summary == null ? 1 : post.Vector.AlignmentWith(alignmentSpace.Summary.Vector);

        Origin.Update(post.Vector, semanticChange * confidence);
    }

    //public void Update(IEnumerable<Post> allPosts)
    //{
    //    if (SummaryVector.IsEmpty)
    //    {
    //        UpdateDescriptiveVector(allPosts);
    //        Vector = SummaryVector;
    //        return;
    //    }

    //    var relevantPosts = GetRelevantPosts(allPosts);

    //    var gradients = relevantPosts.Select(x => x.Vector.Subtract(SummaryVector)).ToList();
    //    Vector = BlossomVector.Sum(gradients).Normalize();

    //    UpdateDescriptiveVector(relevantPosts);
    //}

    List<Post> GetRelevantPosts(IEnumerable<Post> allPosts)
    {
        int k = (int)Math.Floor(Math.Sqrt(allPosts.Count()));

        var angleToSearch = allPosts
            .OrderBy(x => Vector.SimilarityTo(x.Vector))
            .Skip(k - 1)
            .FirstOrDefault()?
            .Vector.SimilarityTo(Vector);

        var relevantPosts = allPosts.Where(x => Vector.SimilarityTo(x.Vector) >= angleToSearch);
        if (RoomType == "User")
            relevantPosts = relevantPosts.Where(x => x.User.Id == User.Id);
        
        return relevantPosts.ToList();
    }

    public void CalculateAnswer(IEnumerable<Post> relevantPosts)
    {
        if (Summary == null || Summary.Vector.IsEmpty)
            return;

        var weightedPosts = relevantPosts.Select(x => x.Vector.Multiply(x.Vector.SimilarityTo(Summary.Vector)));
        Vector = BlossomVector.Sum(weightedPosts).Normalize();
    }

    //void UpdateDescriptiveVector(IEnumerable<Post> posts)
    //{
    //    SummaryVector = BlossomVector.Average(posts.Select(x => x.Vector), x => x.CoherenceWeight);
    //}

    public List<Axis> MaterializeAxes(IEnumerable<Facet> candidates)
    {
        var facets = candidates
            .OrderByDescending(x => x.Vector.CoherenceWeight)
            .Take(2)
            .ToList();

        if (Axes.Count > 0)
            return Axes;

        var x = facets.FirstOrDefault() ?? new(this, BlossomVector.Basis(Vector.Vector.Length, 0));
        var y = facets.Skip(1).FirstOrDefault() ?? x.Orthogonal();
        Facet? z = null;

        var xAxis = new Axis(this, x, "X");
        var yAxis = new Axis(this, y, "Y");
        var zAxis = z == null ? null : new Axis(this, z, "Z");

        Axes = zAxis == null ? [xAxis, yAxis] : [xAxis, yAxis, zAxis];
        return Axes;
    }

    public void ActivateQuest(Quest quest)
    {
        Axes = quest.Axes;
        ActiveQuestId = quest.Id;
    }

    public void DeactivateQuest()
    {
        Axes = [];
        ActiveQuestId = null;
    }
}


