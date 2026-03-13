using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data.Pouch;

public class ReplicationLog : BlossomEntity<string>
{
    [JsonPropertyName("_id")]
    public string PouchId { get; set; } = "";

    [JsonPropertyName("_db")]
    public string Db { get; set; } = "";

    [JsonPropertyName("_history")]
    public List<ReplicationHistory> History { get; set; } = [];

    [JsonPropertyName("_replicator")]
    public string Replicator { get; set; } = "";

    [JsonPropertyName("_session_id")]
    public string SessionId { get; set; } = "";

    [JsonPropertyName("_last_seq")]
    public long LastSeq { get; set; }

    [JsonPropertyName("_version")]
    public int Version { get; set; }

    internal void SetId(string id)
    {
        PouchId = id;
        Id = id;
        Db ??= "sparc";
    }
}
