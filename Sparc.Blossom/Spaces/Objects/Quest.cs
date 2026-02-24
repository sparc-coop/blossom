namespace Sparc.Blossom.Spaces;

public class Quest : BlossomSpace
{
    public string FacetId { get; set; } = "";
    public double Length { get; set; }
    public double Importance { get; set; }
    public BlossomVector NextTurn { get; set; } = new();
    
    public Quest()
    { }
    
    public Quest(string spaceId) : base(spaceId)
    { }
    
    public Quest(BlossomSpace space, BlossomSpace userSpace, Facet facet) : base(space, "Quest")
    {
        User = userSpace.User;
        Vector = facet.Vector.AlignWith(userSpace.Vector, space.Vector);
        Name = facet.Summary?.Name ?? Name;
        Summary = facet.Summary;
        FacetId = facet.Id;

        NextTurn = facet.Vector.Scale(userSpace.Vector, space.Vector);

        Length = NextTurn.Length;
        Importance = facet.Vector.CoherenceWeight * 10;

        MaterializeAxes([facet]);
    }

    public override void MaterializeCoordinates(List<Axis> axes)
    {
        Coordinates = NextTurn.ToCoordinates(axes);
        Distance = axes.FirstOrDefault(x => x.Name == "User")?.Vector.DistanceTo(NextTurn) * lightYearsPerUnit ?? 0;

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
}
