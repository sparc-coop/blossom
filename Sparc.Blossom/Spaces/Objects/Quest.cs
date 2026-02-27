namespace Sparc.Blossom.Spaces;

public class Quest : BlossomSpace
{
    public string FacetId { get; set; } = "";
    public double Importance { get; set; }
    public List<BlossomScoredVector<string>> Signposts { get; set; } = [];
    public BlossomVector NextTurn { get; set; } = new();
    public string? Hint { get; set; }

    public Quest()
    { }
    
    public Quest(string spaceId) : base(spaceId)
    { }
    
    public Quest(BlossomSpace space, BlossomSpace userSpace, Facet facet) : base(space, "Quest")
    {
        User = userSpace.User;
        Vector = facet.Vector.AlignWith(userSpace.Origin, space.Vector);
        Name = facet.Summary?.Name ?? Name;
        Summary = facet.Summary;
        FacetId = facet.Id;
        Signposts = facet.Signposts;

        NextTurn = space.Vector.ProjectOntoAxis(Vector);

        Importance = facet.Vector.CoherenceWeight * 10;

        MaterializeAxes([facet]);
    }

    public void SetSignposts(IEnumerable<Post> posts)
    {
        var scoredPosts = posts.Select(p => new BlossomScoredVector<string>(p.Text!, p.Vector.PositionOnAxis(Vector)));
        // Choose up to 20 signposts distributed across the similarity spectrum
        var min = scoredPosts.Min(p => p.Score);
        var max = scoredPosts.Max(p => p.Score);

        Signposts = scoredPosts
            .GroupBy(p => Math.Floor((p.Score - min) / (max - min + 1e-10) * 20)) // Group into 20 buckets
            .OrderBy(x => x.Key)
            .SelectMany(g => g.Take(1)) // Take one from each bucket
            .Take(20) // Limit to 20 total
            .ToList();
    }

    public string ClosestSignpost(BlossomSpace userSpace)
    {
        var userPosition = userSpace.Origin.PositionOnAxis(Vector);
        return Signposts.OrderBy(s => Math.Abs(s.Score - userPosition)).FirstOrDefault()?.Item ?? "";
    }

    public string NextTurnSignpost(BlossomSpace userSpace)
    {
        var nextTurnPosition = NextTurn.PositionOnAxis(Vector);
        return Signposts.OrderBy(s => Math.Abs(s.Score - nextTurnPosition)).FirstOrDefault()?.Item ?? "";
    }

    public override void MaterializeCoordinates(List<Axis> axes)
    {
        var userAxis = axes.FirstOrDefault(x => x.Name == "User");
        var userRoute = userAxis == null ? NextTurn : NextTurn.Subtract(userAxis.Vector);
        Coordinates = userRoute.ToCoordinates(axes);
        Distance = userAxis?.Vector.AngularDistanceTo(NextTurn, parsecsPerUnit) ?? 0;
    }

    public List<Axis> MaterializeQuestAxes(BlossomSpace space, List<Axis> axes)
    {
        var xAxis = Vector.ProjectOntoPlane(axes[0].Vector, axes[1].Vector);
        var yAxis = xAxis.Perpendicular(axes[0], axes[1]);
        var zAxis = xAxis.Orthogonal(yAxis);
        
        return
        [
            new(space, new Facet(SpaceId) { Vector = xAxis }, "X"),
            new(space, new Facet(SpaceId) { Vector = yAxis }, "Y"),
            new(space, new Facet(SpaceId) { Vector = zAxis }, "Z")
        ];
    }

    public bool IsExitable(BlossomSpace userSpace)
    {
        return userSpace.Vector.AngularDistanceTo(NextTurn, parsecsPerUnit) < 1_000_000_000;
    }
}
