namespace Sparc.Blossom.Spaces;

public class Axis : Facet
{
    public Axis() { }
    
    public Axis(string spaceId) : base(spaceId)
    { }

    public Axis(string name, BlossomSpace userSpace) : base(userSpace, userSpace.Origin)
    {
        Name = name;
    }

    public Axis(BlossomSpace space, Facet facet, string name)
        : base(space, facet.Vector)
    {
        Name = name;
    }

    public string Name { get; set; } = "";
}
