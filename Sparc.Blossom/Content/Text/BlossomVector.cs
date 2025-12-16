using System.Text;
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

    public BlossomVector(float[] vector)
    {
        Vector = vector;
    }

    public string SpaceId { get; init; } = "";
    public string Model { get; init; } = "";
    public float[] Vector { get; init; } = [];
    public string TargetUrl { get; init; } = "";
    public string? Text { get; set; }

    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append('[');
        for (int i = 0; i < Vector.Length; i++)
        {
            str.Append(Vector[i]);
            if (i < Vector.Length - 1)
                str.Append(',');
        }
        str.Append(']');
        return str.ToString();
    }
}
