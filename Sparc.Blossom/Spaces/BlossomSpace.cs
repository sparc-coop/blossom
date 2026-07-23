using Sparc.Blossom.Authentication;
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
    
public class BlossomSpace : BlossomEntity<string>
{
    public string RealmId { get; set; }
    public string? ParentSpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = "Voyage";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public BlossomVector Vector { get; set; } = new();
    public BlossomAvatar User { get; set; } = BlossomUser.System.Avatar;
    public BlossomSummary? Summary { get; set; }
    public BlossomSpaceSettings Settings { get; set; } = new();
    public List<Axis> Axes { get; set; } = [];
    public BlossomVector Origin { get; set; } = new();
    public float Mass => EntityType == "User" ? 10 : 0;

    public string? ActiveQuestId { get; set; }

    [JsonConstructor]
    protected BlossomSpace() : base(Guid.NewGuid().ToString())
    {
        RealmId = "";
    }

    public BlossomSpace(string realmId, string id, BlossomAvatar? user = null, string? roomType = null) : base(id)
    {
        RealmId = realmId;
        Id = id;
        User = user ?? BlossomUser.System.Avatar;
        EntityType = roomType ?? "Ephemeral";
    }

    public void SetSummary(BlossomSummary? summary)
    {
        Summary = summary;

        if (summary != null)
            Name = summary.Name;
    }

    public BlossomUserTrail Add(Post post, Post? previousPost, BlossomSpace alignmentSpace)
    {
        var semanticChange = previousPost == null ? 1 : post.Vector.Subtract(previousPost.Vector).Magnitude();
        var confidence = alignmentSpace.Summary == null ? 1 : post.Vector.AlignmentWith(alignmentSpace.Summary.Vector);

        Origin.Update(post.Vector, semanticChange * confidence);

        return new(alignmentSpace, this);
    }

    public void SetGravitationalForce(IEnumerable<BlossomSpark> objects)
    {
        if (EntityType == "User")
            objects = objects.Where(x => x.User.Id == Id); // User orb only feels gravity from their own posts
        
        // base.SetGravitationalForce(objects);
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
        
        //if (EntityType == "User")
        //    relevantPosts = relevantPosts.Where(x => x.User.Id == User.Id);
        
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
        //Axes = quest.Axes;
        //ActiveQuestId = quest.Id;
    }

    public void DeactivateQuest()
    {
        Axes = [];
        ActiveQuestId = null;
    }
}


