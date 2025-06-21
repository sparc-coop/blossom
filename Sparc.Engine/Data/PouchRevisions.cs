using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchRevisions
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = [];

    [JsonPropertyName("start")]
    public int Start { get; set; }
}
