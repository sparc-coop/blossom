using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public class BlossomSpaceWithVector(BlossomSpace space, BlossomVector vector)
{
    public BlossomSpaceWithVector(BlossomSpace space, float[] vector)
        : this(space, new BlossomVector(space, vector))
    { }

    public BlossomSpace Space { get; set; } = space;
    public BlossomVector Vector { get; set; } = vector;

    public void LinkToSpace(BlossomSpaceWithVector space, List<BlossomSpaceWithVector> axes, bool oneWay = false)
    {
        axes = axes.OrderByDescending(x => x.Vector.CoherenceWeight).ToList();
        var xAxis = axes.FirstOrDefault() ?? new(space.Space, BlossomVector.Basis(1536, 0));
        var yAxis = axes.Skip(1).FirstOrDefault() ?? new(space.Space, BlossomVector.Basis(1536, 1));

        var x = Vector.PositionOnAxis(xAxis.Vector, 0, 1);
        var y = Vector.PositionOnAxis(yAxis.Vector, 0, 1);
        var z = space.Space.Weight ?? 1;

        Space.LinkToSpace(space.Space, x, y, z, oneWay);
    }

    public void LinkToSpaceWithPerspective(BlossomSpaceWithVector space)
    {
        var x = Space.RoomType == "Facet" || Space.RoomType == "Quest"
            ? 1 - Math.Abs(Vector.PositionOnAxis(space.Vector, 0, 1))
            : Vector.PositionOnAxis(space.Vector, 0, 1);

        var y = 0;
        var z = 0;

        Space.LinkToSpace(space.Space, x, y, z);
    }

    public double PositionOnAxis(BlossomSpaceWithVector space)
        => Space.PositionOnAxis(space.Space);
}
