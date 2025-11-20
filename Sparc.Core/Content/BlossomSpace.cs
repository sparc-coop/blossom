using System.Text.Json.Serialization;

namespace Sparc.Blossom;

public class BlossomSpace : BlossomEntity<string>
{
    public string Domain { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }


    [JsonConstructor]
    protected BlossomSpace()
    {
        Id = Guid.NewGuid().ToString();
        Domain = string.Empty;
    }

    public BlossomSpace(string domain)
    {
        Domain = domain;
    }
}

