using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public class BlossomSpaceWithVector(BlossomSpace space, BlossomVector vector)
{
    public BlossomSpaceWithVector(BlossomSpace space, float[] vector)
        : this(space, new BlossomVector(space, vector))
    { }

    public BlossomSpace Space { get; set; } = space;
    public BlossomVector Vector { get; set; } = vector;

    public void Add(BlossomPostWithVector post) => Add(post.Vector);

    public void Add(BlossomVector vector)
    {
        if (Vector.IsEmpty)
            Vector.Update(vector, 1.0);
        else
        {
            var projectionOntoAxis = vector.DotProduct(Vector);
            var projectionOntoOrthogonalSubspace = vector.Subtract(Vector.Multiply(projectionOntoAxis)).Magnitude();
            var weight = Math.Abs(projectionOntoAxis) / (Math.Abs(projectionOntoAxis) + projectionOntoOrthogonalSubspace);

            Vector.Update(vector, weight);
        }
    }
}
