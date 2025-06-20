using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class PouchDatum : BlossomEntity<string>
{
    [JsonConstructor]
    private PouchDatum() : this("", "", "")
    {
    }

    public PouchDatum(string db, string pouchId, string rev) : base($"{pouchId}:{rev}")
    {
        Db = db;
        PouchId = pouchId;
        Rev = rev;
        Data = new()
        {
            { "_id", pouchId },
            { "_rev", rev }
        };
    }

    public PouchDatum(string db, Dictionary<string, object?> data)
        : this(db, data["_id"]!.ToString()!, data["_rev"]!.ToString()!)
    {
        Data = data;
        Seq = data.TryGetValue("_seq", out object? seq) ? seq?.ToString() : null;
        Deleted = data.TryGetValue("_deleted", out object? deleted) && deleted != null && (bool)deleted;
    }

    [JsonPropertyName("_db")]
    public string Db { get; set; }

    [JsonPropertyName("_id")]
    public string PouchId { get; set; }

    [JsonPropertyName("_rev")]
    public string Rev { get; set; }

    [JsonPropertyName("_seq")]
    public string? Seq { get; set; }

    [JsonPropertyName("_deleted")]
    public bool Deleted { get; set; }

    public Dictionary<string, object?> Data { get; set; }

    internal void Update()
    {
        var parts = Rev.Split('-');
        var hash = Hash();
        if (parts.Length != 2 || !int.TryParse(parts[0], out var number))
            Rev = $"1-{hash}";
        else
            Rev = $"{number + 1}-{hash}";

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

    private string Hash()
    {
        // use deterministic MD5 hashing to match Pouch revision algorithm
        
        var sorted = Data.OrderBy(kv => kv.Key, StringComparer.Ordinal);
        
        var sb = new System.Text.StringBuilder();
        foreach (var kv in sorted)
        {
            sb.Append(kv.Key);
            sb.Append('=');
            sb.Append(kv.Value?.ToString() ?? "null");
            sb.Append(';');
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var hash = System.Security.Cryptography.MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
