namespace Sparc.Blossom.Spaces;

public class Axis : Facet
{
    public Axis() { }
    
    public Axis(string spaceId) : base(spaceId)
    { }

    public Axis(BlossomSpace userSpace) : base(userSpace, userSpace.Vector)
    {
    }

    public Axis(BlossomSpace space, Facet facet, string name)
        : base(space, facet.Vector)
    {
        Name = name;
    }

    public string Name { get; set; } = "";
}
