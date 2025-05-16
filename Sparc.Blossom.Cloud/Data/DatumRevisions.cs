using Newtonsoft.Json;

namespace Sparc.Blossom.Cloud.Data
{
    public class DatumRevisions
    {
        [JsonProperty("ids")]
        public List<string> Ids { get; set; }
        [JsonProperty("start")]
        public int Start { get; set; }
    }
}
