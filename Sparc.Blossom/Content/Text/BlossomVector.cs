using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public class BlossomVector : BlossomEntity<string>
{
    [JsonConstructor]
    protected BlossomVector()
    {
    }

    public BlossomVector(string spaceId, string model, float[] vector, string targetUrl) 
        : base(Guid.NewGuid().ToString())
    {
        SpaceId = spaceId;
        Model = model;
        Vector = vector;
        TargetUrl = targetUrl;
    }

    public string SpaceId { get; init; } = "";
    public string Model { get; init; } = "";
    public float[] Vector { get; init; } = [];
    public string TargetUrl { get; init; } = "";
    public string? Text { get; set; }
}
