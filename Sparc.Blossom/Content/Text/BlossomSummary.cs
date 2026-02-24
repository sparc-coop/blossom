using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public record BlossomSummary(string Name, string Topic, string Description, string? LeftTopic = null, string? RightTopic = null) : IVectorizable
{
    [JsonSchemaIgnore]
    public BlossomVector Vector { get; set; } = new();
}