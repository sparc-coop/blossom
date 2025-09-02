using Sparc.Blossom;

namespace Sparc.Blossom.Authentication;

public class SparcDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = Normalize(domain) ?? throw new Exception($"Invalid domain name: {domain}");
    public List<string> Exemptions { get; set; } = [];
    public DateTime? DateConnected { get; set; }
    public DateTime? LastTranslatedDate { get; set; }
    public Dictionary<string, int> PagesPerLanguage { get; set; } = [];
    public int TovikUsage { get; set; }
    public string? TovikUserId { get; set; }
    public string LanguageSelectorPosition { get; set; } = "none";
    public string? LanguageSelectorThemeColor { get; set; }

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
    public string FaviconUri => $"https://{Domain}/favicon.ico";
}