using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public class BlossomSpaceWithVector(BlossomSpace space, BlossomVector vector)
{
    public BlossomSpaceWithVector(BlossomSpace space, float[] vector)
        : this(space, new BlossomVector(space, vector))
    { }

    public BlossomSpace Space { get; set; } = space;
    public BlossomVector Vector { get; set; } = vector;

    public void Add(BlossomPostWithVector post)
    {
        var weight = Vector.IsEmpty 
            ? 1 
            : post.Post.CoherenceWeight * 
                (Vector.Type == "User" ? Space.Settings.HeadspaceVelocity : Space.Settings.SpaceGravity);
        Vector.Add(post.Vector, weight);
    }
}
