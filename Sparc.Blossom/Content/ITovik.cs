using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Content;

public interface ITovik
{
    [Get("/translate/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);

    [Post("/translate/crawl")]
    Task<List<TextContent>> CrawlAsync(TovikCrawlRequest request);

    [Get("/domains/{domain}/settings")]
    Task<SparcDomain> GetDomainSettings([AliasAs("domain")] string domain);
}

public record TovikCrawlRequest(string Domain, List<string> ToLanguages, string FromLanguage = "en");
