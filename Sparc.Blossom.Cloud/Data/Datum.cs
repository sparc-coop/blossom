using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sparc.Blossom.Data;

public class Datum : BlossomEntity<string>
{
    public string? Seq { get; set; }
    public required string Rev { get; set; }

    public bool Deleted { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string DatabaseId { get; set; }
    public string? DocJson { get; set; }

    [NotMapped]
    public IDictionary<string, object> Doc
    {
        get => DocJson == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(DocJson);
        set => DocJson = JsonSerializer.Serialize(value);
    }
}
