using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content.Tovik;

namespace Sparc.Blossom.Content;

public interface ITovik
{
    [Get("/translate/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);

    [Post("/translate/crawl")]
    Task<List<TextContent>> CrawlAsync(TovikCrawlRequest request);

    [Post("/translate/entity")]
    Task<TextContent> TranslateAsync(TovikTranslationRequest request);
}

public record TovikCrawlRequest(string Domain, List<string> ToLanguages, string FromLanguage = "en");
public record TranslationRequest(List<TextContent> Content, string? AdditionalContext = null);
public record TovikTranslationRequest(TextContent Content, TovikTranslationOptions Options);