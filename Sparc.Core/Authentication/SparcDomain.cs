using Sparc.Blossom;

namespace Sparc.Engine;

public class SparcDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = Normalize(domain) ?? throw new Exception($"Invalid domain name: {domain}");
    public List<SparcProduct> Products { get; set; } = [];
    public Dictionary<string, string?> Glossary { get; set; } = new();
    public int TovikUsage { get; set; }

    public bool HasProduct(string policyName) => policyName == "Auth" || Products.Any(p => p.ProductId == policyName);

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
            return uri.Host;
        }
        catch (Exception)
        {
            return null;
        }
    }
}