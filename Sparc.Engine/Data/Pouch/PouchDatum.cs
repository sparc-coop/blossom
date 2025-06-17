using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchDatum : BlossomEntity<string>
{
    [JsonPropertyName("_realmId")]
    public required string RealmId { get; set; }

    [JsonPropertyName("_seq")]
    public string? Seq { get; set; }
    
    [JsonPropertyName("_rev")]
    public required string Rev { get; set; }

    [JsonPropertyName("_deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("_revisions")]
    public PouchRevisions Revisions { get; set; }
}
