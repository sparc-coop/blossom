namespace Sparc.Blossom.Spaces;

public class Constellation : BlossomSpaceObject
{
    public Constellation() { }

    public Constellation(string spaceId) : base(spaceId)
    { }

    public Constellation(BlossomSpace space, BlossomVector vector)
        : base(space, vector)
    {
    }
}
