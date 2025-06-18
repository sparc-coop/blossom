using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchDatum(string db, string type, string pouchId, string rev) 
    : BlossomEntity<string>($"{pouchId}:{rev}")
{
    [JsonPropertyName("_db")]
    public required string Db { get; set; } = db;

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

    internal void Update()
    {
        var parts = Rev.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var number))
            Rev = $"1-{Guid.NewGuid():N}";
        else
            Rev = $"{number + 1}-{Guid.NewGuid():N}";

        SetId(PouchId);
    }

    internal void Delete()
    {
        Deleted = true;
        Update();
    }

    internal void SetId(string pouchId)
    {
        PouchId = pouchId;
        Id = $"{PouchId}:{Rev}";
    }
}
