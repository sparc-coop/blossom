using Refit;
using Sparc.Blossom.Spaces;
using System.Security.Cryptography.X509Certificates;

namespace Sparc.Blossom.Content;

public interface ISparcContent
{
    [Get("/translate/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/content")]
    Task<List<TextContent>> PostAsync(ContentRequest request);

    [Get("/content")]
    Task<List<TextContent>> SearchAsync(string domain, string query);

    [Put("/content")]
    Task<TextContent> UpdateAsync(ContentRequest request);
}

public record CrawlRequest(string Domain, List<string> ToLanguages, string FromLanguage = "en");
public record ExtractGraphRequest(IVectorizable Content, List<SparcEntityType> EntityTypes);
public record ContentRequest(List<TextContent> Content, TranslationOptions Options, string? Referrer = null);
public record ContentResponse(List<TextContentLight> Content, string? ContinuationToken = null);
public record TranslationApiRequest(List<string> Content, TranslationOptions Options);
public record TranslationApiResponse(string OriginalText, string? TranslatedText, string Language);
public record TextContentLight(string Id, string LanguageId, string? Text, string OriginalText)
{
    public TextContentLight(TextContent content)
        : this(content.Id, content.LanguageId, content.Text, content.OriginalText)
    {
    }

    public static List<TextContentLight> From(IEnumerable<TextContent> content) 
        => content.Select(c => new TextContentLight(c)).ToList();
}