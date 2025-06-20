namespace Sparc.Engine;

public interface ITranslator
{
    int Priority { get; }

    Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, IEnumerable<Language> toLanguages, string? additionalContext = null);
    Task<List<Language>> GetLanguagesAsync();
    bool CanTranslate(Language fromLanguage, Language toLanguage);
}