namespace Sparc.Blossom.Spaces;

public class Facet : BlossomSpaceObject
{
    public Facet() { }
    
    public Facet(string spaceId) : base(spaceId)
    { }
    
    public Facet(BlossomSpace space, BlossomVector vector)
        : base(space, vector)
    {
    }

    public bool IsQuestable(BlossomSpace space, BlossomSpace userSpace, double distanceToAnswer)
    {
        var quest = Vector.DotProduct(space.Vector) >= 0 ? Vector : Vector.Multiply(-1);
        var userProjection = quest.DotProduct(userSpace.Vector);
        var answerProjection = quest.DotProduct(space.Vector);
        var userQuest = quest.Multiply(answerProjection - userProjection);

        var lengthThreshold = Math.Min(0.1, distanceToAnswer / 2);
        //var similarityThreshold = 0.8;
        //var similarity = lastMovement.SimilarityTo(quest);

        return userQuest.Length >= lengthThreshold;
    }

    internal Facet Orthogonal()
    {
        var orthogonalVector = Vector.Orthogonal();
        return new Facet(SpaceId) { Vector = orthogonalVector };
    }
}
