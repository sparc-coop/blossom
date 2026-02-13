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

    public bool IsQuestable { get; set; }

    public void CheckForQuest(BlossomSpace space, Headspace user, double distanceToAnswer)
    {
        var quest = Vector.DotProduct(space.Vector) >= 0 ? Vector : Vector.Multiply(-1);
        var userProjection = quest.DotProduct(user.Vector);
        var answerProjection = quest.DotProduct(space.Vector);
        var userQuest = quest.Multiply(answerProjection - userProjection);

        var lengthThreshold = Math.Min(0.1, distanceToAnswer / 2);
        //var similarityThreshold = 0.8;
        //var similarity = lastMovement.SimilarityTo(quest);

        IsQuestable = userQuest.Length >= lengthThreshold;
    }
}
