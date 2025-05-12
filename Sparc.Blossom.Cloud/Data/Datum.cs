namespace Sparc.Blossom.Data;

public class Datum : BlossomEntity<string>
{
    public string? Seq { get; set; }
    public required string Rev { get; set; }

    public bool Deleted { get; set; }
    public string TenantId { get; internal set; }
    public string UserId { get; internal set; }
    public string DatasetId { get; internal set; }
}
