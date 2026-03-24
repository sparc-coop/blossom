namespace Sparc.Blossom.Spaces;

public class Quest : BlossomSpace
{
    public string? Hint { get; set; }

    public Quest()
    { }
    
    public Quest(string spaceId) : base(spaceId)
    { }
    
    public Quest(BlossomSpace space, BlossomSpace userSpace) : base(space, "Quest")
    {
        User = userSpace.User;
    }

    public List<QuestPath> Travel(BlossomVector initialPoint, List<BlossomSpaceObject> objects, int maxIterations = 100, float stepSize = 0.1f, float tolerance = 0.01f)
    {
        List<QuestPath> path = [new(this, 0, initialPoint)];
        for (int i = 1; i < maxIterations; i++)
        {
            var previous = path[i - 1];
            var force = GradientForce(objects, previous.Point);
            var magnitude = force.Magnitude();
            if (magnitude < tolerance)
                break;

            var nextStep = force.Multiply(stepSize);
            QuestPath next = new(this, i, nextStep, previous);
            path.Add(next);

            var closestObject = objects.OrderBy(o => o.Vector.DistanceTo(next.Point)).FirstOrDefault();
            next.Signpost = closestObject?.Vector.Text ?? "";
        }

        Vector = path.LastOrDefault()?.Vector ?? initialPoint;

        return path;
    }

    //public void SetSignposts(IEnumerable<Post> posts)
    //{
    //    var scoredPosts = posts.Select(p => new BlossomScoredVector<string>(p.Text!, p.Vector.PositionOnAxis(Vector)));
    //    // Choose up to 20 signposts distributed across the similarity spectrum
    //    var min = scoredPosts.Min(p => p.Score);
    //    var max = scoredPosts.Max(p => p.Score);

    //    Signposts = scoredPosts
    //        .GroupBy(p => Math.Floor((p.Score - min) / (max - min + 1e-10) * 20)) // Group into 20 buckets
    //        .OrderBy(x => x.Key)
    //        .SelectMany(g => g.Take(1)) // Take one from each bucket
    //        .Take(20) // Limit to 20 total
    //        .ToList();
    //}

    //public override void MaterializeCoordinates(List<Axis> axes)
    //{
    //    var userAxis = axes.FirstOrDefault(x => x.Name == "User");
    //    var userRoute = userAxis == null ? NextTurn : NextTurn.Subtract(userAxis.Vector);
    //    Coordinates = userRoute.ToCoordinates(axes);
    //    Distance = userAxis?.Vector.AngularDistanceTo(NextTurn, parsecsPerUnit) ?? 0;
    //}

    //public bool IsExitable(BlossomSpace userSpace)
    //{
    //    return userSpace.Vector.AngularDistanceTo(NextTurn, parsecsPerUnit) < 1_000_000_000;
    //}


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

}
