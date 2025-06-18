using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchDatum(string realmId, string type, string pouchId, string rev) 
    : BlossomEntity<string>($"{pouchId}:{rev}")
{
    [JsonPropertyName("_realmId")]
    public required string RealmId { get; set; } = realmId;

    [JsonPropertyName("_type")]
    public required string Type { get; set; } = type;

    [JsonPropertyName("_id")]
    public required string PouchId { get; set; } = pouchId;

    [JsonPropertyName("_rev")]
    public required string Rev { get; set; } = rev;


    [JsonPropertyName("_seq")]
    public string? Seq { get; set; }
    
    
    [JsonPropertyName("_deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("_revisions")]
    public PouchRevisions Revisions { get; set; } = new();
}
