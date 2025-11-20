using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public class BlossomSpace : BlossomEntity<string>
{
    public string Domain { get; set; }

    [JsonConstructor]
    protected BlossomSpace()
    {
        Id = string.Empty;
        Domain = string.Empty;
    }

    public BlossomSpace(string domain)
    {
        Domain = domain;
    }
}

