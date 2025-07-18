using Sparc.Blossom;

namespace Sparc.Engine;

public class SparcDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public int TotalUsage = new Random().Next(1, 100000);
    public List<string> Exemptions{ get; set; } = new();
    public DateTime? DateConnected { get; set; }
    public string Domain { get; set; } = Normalize(domain) ?? throw new Exception($"Invalid domain name: {domain}");
    public int TovikUsage { get; set; }
    public string? TovikUserId { get; set; }

    public static string? Normalize(string domain)
    {
        domain = domain.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(domain) || (!domain.Contains("localhost") && !domain.Contains(".")))
            return null;

        if (!domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            domain = "https://" + domain;

        try
        {
            var uri = new Uri(domain);
            domain = uri.Host + (!uri.IsDefaultPort ? $":{uri.Port}" : "");
            return domain;
        }
        catch (Exception)
        {
            return null;
        }
    }
}