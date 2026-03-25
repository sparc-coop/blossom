namespace Sparc.Blossom.Spaces;

public class QuestPath : BlossomSpaceObject
{
    public string Signpost { get; set; } = "";
    public int Index { get; set; }
    public BlossomVector Point { get; set; } = BlossomVector.Zero(1024);

    public QuestPath()
    { }

    public QuestPath(string spaceId) : base(spaceId)
    {
    }

    public QuestPath(Quest quest, int index, BlossomVector vector, QuestPath? previousPath = null, string? signpost = null) : base(quest.Id)
    {
        Point = vector;
        Index = index;
        User = quest.User;
        Vector = previousPath == null ? vector : previousPath.Vector.Subtract(vector);

        if (signpost != null)
            Signpost = signpost;
    }

    public static QuestPath? Closest(List<QuestPath> Paths, BlossomSpace userSpace)
    {
        return Paths.OrderBy(s => s.Vector.AngularDistanceTo(userSpace.Vector, parsecsPerUnit)).FirstOrDefault();
    }
}
