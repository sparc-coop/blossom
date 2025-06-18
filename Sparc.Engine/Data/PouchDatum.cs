using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchDatum(string db, string pouchId, string rev) 
    : BlossomEntity<string>($"{pouchId}:{rev}")
{
    public PouchDatum(string db, Dictionary<string, object?> data) 
        : this(db, data["_id"]!.ToString()!, data["_rev"]!.ToString()!)
    {
        Data = data;
        Seq = data.TryGetValue("_seq", out object? seq) ? seq?.ToString() : null;
        Deleted = data.TryGetValue("_deleted", out object? deleted) && deleted != null && (bool)deleted;
    }

    [JsonPropertyName("_db")]
    public string Db { get; set; } = db;

    [JsonPropertyName("_id")]
    public string PouchId { get; set; } = pouchId;

    [JsonPropertyName("_rev")]
    public string Rev { get; set; } = rev;

    [JsonPropertyName("_seq")]
    public string? Seq { get; set; }
    
    [JsonPropertyName("_deleted")]
    public bool Deleted { get; set; }

    public Dictionary<string, object?> Data { get; set; } = new()
    {
        { "_id", pouchId },
        { "_rev", rev }
    };

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
