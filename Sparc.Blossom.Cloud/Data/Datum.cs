namespace Sparc.Blossom.Cloud.Data;

public class Datum : BlossomEntity<string>
{
    public string? Seq { get; set; }
    public required string Rev { get; set; }
}
