using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data.Pouch;

public class PouchRevisions
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = [];

    [JsonPropertyName("start")]
    public int Start { get; set; }
}
