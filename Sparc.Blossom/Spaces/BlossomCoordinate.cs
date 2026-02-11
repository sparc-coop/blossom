namespace Sparc.Blossom.Content;

public record BlossomCoordinate(string Id, string Name, string Type, double X, double Y, double Z, double? Length = null)
{
    public BlossomSummary? Summary { get; set; }
    public string? ConnectTo { get; set; }
}
