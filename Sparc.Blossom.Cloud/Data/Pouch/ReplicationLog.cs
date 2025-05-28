using Newtonsoft.Json;

namespace Sparc.Blossom.Data.Pouch
{
    public class ReplicationLog : BlossomEntity<string>
    {
        [JsonProperty("_id")]
        public string PouchId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("_tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("_userId")]
        public string UserId { get; set; }

        [JsonProperty("_databaseId")]
        public string DatabaseId { get; set; }

        [JsonProperty("_history")]
        public List<ReplicationHistory> History { get; set; }

        [JsonProperty("_replicator")]
        public string Replicator { get; set; }

        [JsonProperty("_session_id")]
        public string SessionId { get; set; }

        [JsonProperty("_last_seq")]
        public long LastSeq { get; set; }

        [JsonProperty("_version")]
        public int version { get; set; }

        public void SetFromPouch(string databaseId, string documentId)
        {
            DatabaseId = databaseId;
            Id = documentId;
        }

    }
}
