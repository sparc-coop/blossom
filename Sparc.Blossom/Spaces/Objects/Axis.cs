namespace Sparc.Blossom.Spaces;

public class Axis : Facet
{
    public Axis(string spaceId) : base(spaceId)
    { }
    
    public Axis(BlossomSpace space, Facet facet, string name)
        : base(space, facet.Vector)
    {
        IsQuestable = facet.IsQuestable;
        Name = name;
    }

    public string Name { get; set; } = "";
}
