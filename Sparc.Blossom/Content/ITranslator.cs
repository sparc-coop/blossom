using Sparc.Blossom.Content.Tovik;

namespace Sparc.Blossom.Content;

public interface ITranslator
{
    int Priority { get; }

    Task<TextContent> TranslateAsync(TextContent message, TovikTranslationOptions options);
    Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options);
    Task<List<Language>> GetLanguagesAsync();
    bool CanTranslate(Language fromLanguage, Language toLanguage);
}