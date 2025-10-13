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

    public async Task<T?> TranslateAsync<T>(TextContent content, TovikTranslationOptions? options = null)
        => await TranslateAsync<T, T>(content, options);

    public async Task<T?> TranslateAsync<T, TSchema>(TextContent content, TovikTranslationOptions? options = null)
    {
        options ??= new();
        options.Schema = new(typeof(TSchema));
        var request = new TovikTranslationRequest(content, options);
        var result = await TranslateAsync(request);
        var obj = result.Cast<T>();
        return obj;
    }
}

public record TovikCrawlRequest(string Domain, List<string> ToLanguages, string FromLanguage = "en");
public record TranslationRequest(List<TextContent> Content, string? AdditionalContext = null, string? Model = null);
public record Visit(string Domain, string Path);
public record TovikTranslationRequest(TextContent Content, TovikTranslationOptions Options);