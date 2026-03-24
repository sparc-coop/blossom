namespace Sparc.Blossom.Spaces;

public class Facet : BlossomSpaceObject
{
    public Facet() { }
    
    public Facet(string spaceId) : base(spaceId)
    { }
    
    public Facet(BlossomSpace space, BlossomVector vector, IEnumerable<Post>? posts = null)
        : base(space, vector)
    {
        Vector = vector.AlignWith(space.Vector);
        
        if (posts != null)
            SetSignposts(posts);
    }

    public List<BlossomScoredVector<string>> Signposts { get; set; } = [];

    //public bool IsQuestable(BlossomSpace space, BlossomSpace userSpace, double distanceToAnswer)
    //{
    //    var quest = new Quest(space, userSpace, this);

    //    var lengthThreshold = Math.Min(0.1, distanceToAnswer / 2);
    //    //var similarityThreshold = 0.8;
    //    //var similarity = lastMovement.SimilarityTo(quest);

    //    return quest.Vector.Length >= lengthThreshold;
    //}

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

    internal Facet Orthogonal()
    {
        var orthogonalVector = Vector.Orthogonal();
        return new Facet(SpaceId) { Vector = orthogonalVector };
    }

    public void AlignWith(BlossomSpace space)
    {
        Vector = Vector.AlignWith(space.Vector);
        //if (newVector.Vector[0] != Vector.Vector[0]) // Vector has flipped
        //{
        //    Vector = newVector;
        //    Signposts = Signposts.Select(x => x with { Score = -x.Score })
        //        .OrderBy(x => x.Score)
        //        .ToList();
        //}
    }
}
