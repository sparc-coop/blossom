using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public interface ISparcContent
{
    [Get("/content/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);

    [Post("/content/crawl")]
    Task<List<TextContent>> CrawlAsync(CrawlRequest request);

    [Post("/content")]
    Task<List<TextContent>> PostAsync(PostContentRequest request);
    [Post("/content/{id}")]
    Task<TextContent> PostAsync(string id, PostSingleContentRequest request);

    public async Task<TextContent> PostAsync(TextContent content, TranslationOptions? options = null)
    {
        var request = new PostSingleContentRequest(content, options);
        var result = await PostAsync(content.Id, request);
        return result;
    }

    public async Task<T?> PostAsync<T>(TextContent content, TranslationOptions? options = null)
        => await PostAsync<T, T>(content, options);

    public async Task<T?> PostAsync<T, TSchema>(TextContent content, TranslationOptions? options = null)
    {
        options ??= new();
        options.Schema = new(typeof(TSchema));
        var request = new PostSingleContentRequest(content, options);
        var result = await PostAsync(content.Id, request);
        var obj = result.Cast<T>();
        return obj;
    }

    [Post("/translate/entity")]
    Task<List<TextContent>> TranslateAsync(TranslationRequest request);

    public async Task<T?> TranslateAsync<T>(TextContent content, TranslationOptions? options = null)
        => await TranslateAsync<T, T>(content, options);

    public async Task<T?> TranslateAsync<T, TSchema>(TextContent content, TranslationOptions? options = null)
    {
        options ??= new();
        options.Schema = new(typeof(TSchema));
        var request = new TranslationRequest([content], options);
        var result = await TranslateAsync(request);
        var obj = result.Cast<T>().FirstOrDefault();
        return obj;
    }

    [Post("/content/graph")]
    Task<List<SparcEntity>> ExtractGraphAsync(ExtractGraphRequest request);
}

public record PostSingleContentRequest(TextContent Content, TranslationOptions? Options = null);
public record PostContentRequest(List<TextContent> Content, TranslationOptions? Options = null); 
public record CrawlRequest(string Domain, List<string> ToLanguages, string FromLanguage = "en");
public record ExtractGraphRequest(IVectorizable Content, List<SparcEntityType> EntityTypes);
public record Visit(string Domain, string Path);
public record TranslationRequest(List<TextContent> Content, TranslationOptions Options);
public record TranslationApiRequest(List<string> Content, TranslationOptions Options);
public record TranslationApiResponse(string OriginalText, string? TranslatedText, string Language);
