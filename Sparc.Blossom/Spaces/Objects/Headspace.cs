namespace Sparc.Blossom.Spaces;

public class Headspace : BlossomSpaceObject
{
    public Headspace()
    { }
    
    public Headspace(string spaceId) : base(spaceId)
    {
    }

    public Headspace(BlossomSpace space, Post post, Headspace? previousHeadspace = null) : base(space, post.Vector)
    {
        User = post.User;

        if (previousHeadspace != null)
        {
            Vector = previousHeadspace.Vector.Add(post.Vector);
            ActiveQuestId = previousHeadspace.ActiveQuestId;
        }
    }

    public string? ActiveQuestId { get; set; }
}
