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
}
