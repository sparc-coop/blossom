using Sparc.Blossom.Cloud.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sparc.Blossom.Data;

public class Datum : BlossomEntity<string>
{
    public string? Seq { get; set; }
    public required string _rev { get; set; }

    public bool _deleted { get; set; }
    public DatumRevisions _revisions { get; set; }

    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string DatabaseId { get; set; }

    public Datum()
    {
        Data = new Dictionary<string, object>();
    }

    public T Get<T>(string key)
    {
        if (Data.TryGetValue(key, out object val))
        {
            if (val is JsonElement el)
            {
                try
                {
                    var json = el.GetRawText();
                    return System.Text.Json.JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return default;
                }
            }

            var item = (T)val;
            return item;
        }

        return default;
    }

    public void Set<U>(string key, U item) where U : class
    {
        if (Data.ContainsKey(key))
            Data[key] = item;
        else
            Data.TryAdd(key, item);
    }

    public Dictionary<string, object> Data { get; set; }

    public string? DocJson { get; set; }

    [NotMapped]
    public IDictionary<string, object> Doc
    {
        get => DocJson == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(DocJson);
        set => DocJson = JsonSerializer.Serialize(value);
    }
}
