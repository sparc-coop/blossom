using Newtonsoft.Json;

namespace Sparc.Blossom.Data;

public class PouchRevisions
{
    [JsonProperty("ids")]
    public List<string> Ids { get; set; }

    [JsonProperty("start")]
    public int Start { get; set; }
}
