using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Spaces;

public class Quest : BlossomSpaceObject
{
    public Quest()
    { }
    
    public Quest(string spaceId) : base(spaceId)
    { }
    
    public Quest(BlossomSpace space, Facet facet, BlossomAvatar avatar)
        : base(space, facet.Vector)
    {
        User = avatar;
        Name = facet.Summary?.RightTopic ?? facet.Summary?.Name ?? Name;
        Summary = facet.Summary;
    }

    public string Name { get; set; } = "";

    public List<Axis> MaterializeAxes(BlossomSpace space, List<BlossomSpaceObject> gameObjects, List<Axis> axes)
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
