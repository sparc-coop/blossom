namespace Sparc.Blossom.Spaces;

public class BlossomUserTrail : BlossomSpaceObject
{
    public BlossomUserTrail()
    { }
    
    public BlossomUserTrail(string spaceId) : base(spaceId)
    {
    }

    public BlossomUserTrail(BlossomSpace space, BlossomSpace previousHeadspace) : base(space, previousHeadspace.Vector)
    {
        User = previousHeadspace.User;
    }
}
