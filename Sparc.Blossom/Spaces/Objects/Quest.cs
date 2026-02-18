namespace Sparc.Blossom.Spaces;

public class Quest : BlossomSpace
{
    public string FacetId { get; set; } = "";
    
    public Quest()
    { }
    
    public Quest(string spaceId) : base(spaceId)
    { }
    
    public Quest(BlossomSpace space, BlossomSpace userSpace, Facet facet) : base(space, "Quest")
    {
        User = userSpace.User;
        Vector = facet.Vector;
        Name = facet.Summary?.RightTopic ?? facet.Summary?.Name ?? Name;
        Summary = facet.Summary;
        FacetId = facet.Id;

        var quest = Vector.DotProduct(space.Vector) >= 0 ? Vector : Vector.Multiply(-1);
        var userProjection = quest.DotProduct(userSpace.Vector);
        var answerProjection = quest.DotProduct(space.Vector);
        Vector = quest.Multiply(answerProjection - userProjection);

        MaterializeAxes([facet]);
    }

    public List<Axis> MaterializeQuestAxes(BlossomSpace space, List<Axis> axes)
    {
        var xAxis = Vector.ProjectOntoPlane(axes[0].Vector, axes[1].Vector);
        var yAxis = xAxis.Perpendicular(axes[0], axes[1]);
        var zAxis = BlossomVector.Basis(xAxis.Vector.Length, 2).Orthogonal(xAxis, yAxis);
        
        return
        [
            new(space, new Facet(SpaceId) { Vector = xAxis }, "X"),
            new(space, new Facet(SpaceId) { Vector = yAxis }, "Y"),
            new(space, new Facet(SpaceId) { Vector = zAxis }, "Z")
        ];
    }
}
