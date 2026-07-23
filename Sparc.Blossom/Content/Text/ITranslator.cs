namespace Sparc.Blossom.Content;

public interface ITranslator
{
    int Priority { get; }

    Task<List<TextContent>> TranslateAsync(ContentRequest request);
    Task<List<Language>> GetLanguagesAsync();
    bool CanTranslate(Language fromLanguage, Language toLanguage);
}

public interface ILanguageDetector
{
    Task<Language?> DetectLanguageAsync(List<TextContent> content, bool forceReload = false);
}