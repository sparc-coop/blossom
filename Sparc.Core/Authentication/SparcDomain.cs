using Sparc.Blossom.Billing;
using Sparc.Core;

namespace Sparc.Blossom.Authentication;

public class SparcDomain(string domain) : BlossomEntity<string>(BlossomHash.MD5(domain))
{
    public string Domain { get; set; } = Normalize(domain) ?? throw new Exception($"Invalid domain name: {domain}");
    public List<string> Exemptions { get; set; } = [];
    public DateTime? DateConnected { get; set; }
    public DateTime? LastTranslatedDate { get; set; }
    public string? LastTranslatedLanguage { get; set; }
    public Dictionary<string, int> PagesPerLanguage { get; set; } = [];
    public int TovikUsage { get; set; }
    public string? TovikUserId { get; set; }
    public List<SparcProduct> Products { get; set; } = [];

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

    public async Task<bool> VerifyAsync()
    {
        var htmlToLookFor = "tovik.js";
        var html = await new HttpClient().GetStringAsync($"https://{Domain}");

        if (html.Contains(htmlToLookFor))
        {
            DateConnected = DateTime.UtcNow;
            return true;
        }
        else
        {
            DateConnected = null;
            return false;
        }
    }

    public Uri ToUri() => new($"https://{Domain}/");
    public static Uri? ToNormalizedUri(string url)
    {
        url = url.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(url) || (!url.Contains("localhost") && !url.Contains(".")))
            return null;

        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        try
        {
            var uri = new Uri(url);
            return uri;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public SparcProduct? Product(string productId) => Products.FirstOrDefault(x => x.ProductId == productId);

    public SparcProduct AddProduct(string productId)
    {
        var existing = Products.FirstOrDefault(x => x.ProductId == productId);
        if (existing != null)
            return existing;

        var product = new SparcProduct(productId);
        Products.Add(product);

        return product;
    }

    public bool HasProduct(string productName)
    {
        return Products.Any(x => x.ProductId.Equals(productName, StringComparison.OrdinalIgnoreCase));
    }

    public void Fulfill(SparcProduct product, string userId)
    {
        var existing = Product(product.ProductId);
        if (existing != null)
        {
            existing.MaxUsage += product.MaxUsage;
            existing.OrderIds.AddRange(product.OrderIds);
        }
        else
            Products.Add(product);

        TovikUserId = userId;
    }

    public string FaviconUri => $"https://{Domain}/favicon.ico";
}