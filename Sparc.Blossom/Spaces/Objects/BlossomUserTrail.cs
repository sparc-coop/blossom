namespace Sparc.Blossom.Spaces;

public class BlossomUserTrail : BlossomSpark
{
    public BlossomUserTrail()
    { }
    
    public BlossomUserTrail(string spaceId) : base(spaceId)
    {
    }

    public BlossomUserTrail(BlossomSpace space, BlossomSpace previousHeadspace) : base(space, previousHeadspace.Origin)
    {
        User = previousHeadspace.User;
    }
}
