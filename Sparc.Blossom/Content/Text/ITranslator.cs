namespace Sparc.Blossom.Content;

public interface ITranslator
{
    int Priority { get; }

    Task<TextContent> TranslateAsync(TextContent message, TranslationOptions options);
    Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TranslationOptions options);
    Task<List<Language>> GetLanguagesAsync();
    bool CanTranslate(Language fromLanguage, Language toLanguage);
}