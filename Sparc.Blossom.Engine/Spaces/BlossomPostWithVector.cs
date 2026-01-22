namespace Sparc.Blossom.Content;

public class BlossomPostWithVector(BlossomPost post, BlossomVector vector)
{
    public BlossomPost Post { get; set; } = post;
    public BlossomVector Vector { get; set; } = vector;

    public void UpdateCoherence(List<BlossomVector> neighbors)
    {
        Vector.CalculateCoherenceWeight(neighbors);
        Post.CoherenceWeight = Vector.CoherenceWeight;
        //post.UserMovementWeight = post.CoherenceWeight * Math.Max(0, spaceVector.SimilarityTo(postVector) ?? 0);
    }

    public void LinkToSpace(BlossomSpaceWithVector space, List<BlossomSpaceWithVector> axes)
    {
        axes = axes.OrderByDescending(x => x.Vector.CoherenceWeight).ToList();
        var xAxis = axes.FirstOrDefault() ?? new(space.Space, BlossomVector.Basis(1536, 0));
        var yAxis = axes.Skip(1).FirstOrDefault() ?? new(space.Space, BlossomVector.Basis(1536, 1));

        var x = Vector.PositionOnAxis(xAxis.Vector, 0, 1);
        var y = Vector.PositionOnAxis(yAxis.Vector, 0, 1);
        var z = Vector.CoherenceWeight;

        Post.LinkToSpace(space.Space, x, y, z);
    }


    public void LinkToSpaceWithPerspective(BlossomSpaceWithVector space, BlossomSpaceWithVector? primaryFacet = null, BlossomSpaceWithVector? secondaryFacet = null)
    {
        var vectorFromThePerspectiveOfSpace = Vector.Subtract(space.Vector);

        var xAxisVector = primaryFacet == null
            ? vectorFromThePerspectiveOfSpace.OrthogonalizedAxis(BlossomVector.Basis(1536, 0), space.Vector)
            : vectorFromThePerspectiveOfSpace.OrthogonalizedAxis(primaryFacet.Vector, space.Vector);

        var yAxis = secondaryFacet == null
            ? vectorFromThePerspectiveOfSpace.OrthogonalizedAxis(BlossomVector.Basis(1536, 1), space.Vector)
            .OrthogonalizedAxis(BlossomVector.Basis(1536, 1), xAxisVector)
            : vectorFromThePerspectiveOfSpace.OrthogonalizedAxis(secondaryFacet.Vector, space.Vector);

        var x = vectorFromThePerspectiveOfSpace.PositionOnAxis(xAxisVector, 0, 1);
        var y = vectorFromThePerspectiveOfSpace.PositionOnAxis(yAxis, 0, 1);
        var z = vectorFromThePerspectiveOfSpace.CoherenceWeight;

        Post.LinkToSpace(space.Space, x, y, z);
    }

    public double PositionOnAxis(BlossomSpaceWithVector space)
        => Vector.PositionOnAxis(space.Vector);
}
