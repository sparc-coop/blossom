namespace Sparc.Blossom.Content;

public record BlossomCoordinate(string Id, string Name, string Type, double X, double Y, double Z)
{
    public BlossomSummary? Summary { get; set; }
}
