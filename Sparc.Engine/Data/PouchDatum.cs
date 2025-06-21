using Sparc.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public record PouchRevisionAdded(PouchDatum Datum) : BlossomEvent<PouchDatum>(Datum);
public class PouchDatum(string db, string pouchId, string rev) : BlossomEntity<string>($"{pouchId}:{rev}")
{
    [JsonConstructor]
    private PouchDatum() : this("", "", "")
    {
    }

    public PouchDatum(string db, Dictionary<string, object?> data)
        : this(db, data["_id"]!.ToString()!, data["_rev"]!.ToString()!)
    {
        CreatedDate = DateTime.Now;
        Update(data);
        Broadcast(new PouchRevisionAdded(this));
    }

    [JsonPropertyName("_db")]
    public string Db { get; set; } = db;

    [JsonPropertyName("_id")]
    public string PouchId { get; set; } = pouchId;

    [JsonPropertyName("_rev")]
    public string Rev { get; set; } = rev;

    [JsonPropertyName("_seq")]
    public string? Seq { get; set; } = DateTime.UtcNow.Ticks.ToString();

    [JsonPropertyName("_deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("_revisions")]
    public PouchRevisions? Revisions { get; set; }

    [JsonPropertyName("_createdDate")]
    public DateTime CreatedDate { get; set; }

    public Dictionary<string, object?> Data { get; set; } = [];

    internal void IncrementRevision()
    {
        var parts = Rev.Split('-');
        if (parts.Length != 2)
        {
            Rev = $"1-{Hash()}";
        }
        else
        {
            var oldHash = parts[1];
            var hash = Hash();
            if (oldHash == hash)
                return; // no change in data, no need to update revision

            if (!int.TryParse(parts[0], out var number))
                Rev = $"1-{hash}";
            else
                Rev = $"{number + 1}-{hash}";
        }

        SetId(PouchId);
        Broadcast(new PouchRevisionAdded(this));
    }

    private void SetRevisions()
    {
        var start = Rev.Split('-')[0];
        var id = Rev.Split('-')[1];

        Revisions ??= new PouchRevisions();
        Revisions.Start = int.Parse(start);
        if (!Revisions.Ids.Contains(id))
            Revisions.Ids.Insert(0, Rev.Split('-')[1]);
    }

    internal T? Cast<T>() where T : BlossomEntity<string>
    {
        if (!Data.TryGetValue("$type", out var type) || type is not string typeString || typeString != typeof(T).Name)
            return default;

        var result = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Data));
        if (result != null)
            result.Id = PouchId;
        return result;
    }

    internal void Update(Dictionary<string, object?> data)
    {
        if (data.TryGetValue("_rev", out var rev) && rev is string revString)
            Rev = revString;

        if (data.TryGetValue("_id", out var id) && id is string pouchId)
            PouchId = pouchId;

        if (data.TryGetValue("_seq", out var seq))
            Seq = seq?.ToString();

        Deleted = data.TryGetValue("_deleted", out var deleted) && deleted != null && (bool)deleted;

        if (data.TryGetValue("_revisions", out var revisions) && revisions is JsonElement el)
            Revisions = el.Deserialize<PouchRevisions>();
        else
            SetRevisions();

        Data = FromDictionary(data);
    }

    internal static PouchDatum Create<T>(string db, T data) where T : BlossomEntity<string>
    {
        var datum = new PouchDatum(db, data.Id, "");
        datum.Update(data);
        return datum;
    }

    internal void Update<T>(T data)
    {
        Data = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(data))
            ?? throw new InvalidOperationException("Failed to serialize data to dictionary");

        IncrementRevision();
    }

    internal void Delete()
    {
        Deleted = true;
        IncrementRevision();
    }

    internal void SetId(string pouchId)
    {
        PouchId = pouchId;
        Id = $"{PouchId}:{Rev}";
        SetRevisions();
    }

    internal Dictionary<string, object?> ToDictionary()
    {
        var dict = new Dictionary<string, object?>(Data)
        {
            ["_id"] = PouchId,
            ["_rev"] = Rev,
            ["_deleted"] = Deleted
        };

        if (Revisions != null)
            dict["_revisions"] = Revisions;

        return dict;
    }

    internal static Dictionary<string, object?> FromDictionary(Dictionary<string, object?> data)
    {
        var dict = new Dictionary<string, object?>(data);
        var internalKeys = dict.Keys.Where(x => x.StartsWith('_')).ToList();
        foreach (var internalKey in internalKeys)
            dict.Remove(internalKey);

        return dict;
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

        return BlossomHash.MD5(sb.ToString());
    }
}
