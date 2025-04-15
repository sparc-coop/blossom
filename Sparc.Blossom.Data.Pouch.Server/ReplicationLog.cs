using Newtonsoft.Json;

namespace Sparc.Blossom.Data.Pouch.Server
{
    public class ReplicationLog
    {
        [JsonProperty("_id")]
        public string PouchId { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }

        public string DatasetId { get; set; }

        [JsonProperty("history")]
        public List<ReplicationHistory> History { get; set; }

        public void SetFromPouch(string datasetId, string documentId)
        {
            DatasetId = datasetId;
            Id = documentId;
        }

        public string replicator { get; set; }
        public string session_id { get; set; }

        public long last_seq { get; set; }
        public int version { get; set; }
    }
}
