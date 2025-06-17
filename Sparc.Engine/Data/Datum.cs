using Newtonsoft.Json;
using System.Text.Json;


namespace Sparc.Blossom.Data;

public class Datum : BlossomEntity<string>
{
    [JsonProperty("_seq")]
    public string? Seq { get; set; }
    
    [JsonProperty("_rev")]
    public required string Rev { get; set; }

    [JsonProperty("_deleted")]
    public bool Deleted { get; set; }

    [JsonProperty("_revisions")]
    public DatumRevisions Revisions { get; set; }

    [JsonProperty("_tenantId")]
    public string TenantId { get; set; }

    [JsonProperty("_userId")]
    public string UserId { get; set; }

    [JsonProperty("_databaseId")]
    public string DatabaseId { get; set; }

    [JsonProperty("_doc")]
    public Dictionary<string, object> Doc { get; set; }

    public Datum()
    {
        Doc = new Dictionary<string, object>();
    }

    public T Get<T>(string key)
    {
        if (Doc.TryGetValue(key, out object val))
        {
            if (val is JsonElement el)
            {
                try
                {
                    var json = el.GetRawText();
                    return JsonConvert.DeserializeObject<T>(json);
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

    public void Set<U>(string key, U item)
    {
        // If U is a JsonElement.ValueKind, store the value as its underlying type
        if (item is JsonElement el)
        {
            object value;
            switch (el.ValueKind)
            {
                case JsonValueKind.String:
                    value = el.GetString();
                    break;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out long l))
                        value = l;
                    else if (el.TryGetDouble(out double d))
                        value = d;
                    else
                        value = el.GetRawText();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = el.GetBoolean();
                    break;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    value = el.GetRawText();
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    value = null;
                    break;
            }
            if (Doc.ContainsKey(key))
                Doc[key] = value;
            else
                Doc.TryAdd(key, value);
        }
        else
        {
            if (Doc.ContainsKey(key))
                Doc[key] = item;
            else
                Doc.TryAdd(key, item);
        }
    }

    

    //public string? DocJson { get; set; }

    //[NotMapped]
    //public IDictionary<string, object> Doc
    //{
    //    get => DocJson == null ? null : JsonConvert.DeserializeObject<Dictionary<string, object>>(DocJson);
    //    set => DocJson = JsonConvert.SerializeObject(value);
    //}
}
