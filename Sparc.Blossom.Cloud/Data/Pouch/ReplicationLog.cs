using Newtonsoft.Json;

namespace Sparc.Blossom.Data.Pouch
{
    public class ReplicationLog : BlossomEntity<string>
    {
        [JsonProperty("_id")]
        public string PouchId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string DatasetId { get; set; }

        [JsonProperty("history")]
        public List<ReplicationHistory> History { get; set; }

        public string replicator { get; set; }
        public string session_id { get; set; }

        public long last_seq { get; set; }
        public int version { get; set; }

        public void SetFromPouch(string datasetId, string documentId)
        {
            DatasetId = datasetId;
            Id = documentId;
        }

    }
}
