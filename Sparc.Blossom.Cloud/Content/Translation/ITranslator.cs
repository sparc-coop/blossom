namespace Sparc.Blossom.Cloud;

public interface ITranslator
{
    int Priority { get; }

    Task<List<Content>> TranslateAsync(IEnumerable<Content> messages, IEnumerable<Language> toLanguages, string? additionalContext = null);
    Task<List<Language>> GetLanguagesAsync();
    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    async Task<Language?> GetLanguageAsync(Language language) => await GetLanguageAsync(language.Id);

    async Task<List<Content>> TranslateAsync(IEnumerable<Content> messages, Language toLanguage, string? additionalContext = null)
    {
        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        return await TranslateAsync(messages, [language], additionalContext);
    }

    public async Task<Content?> TranslateAsync(Content message, Language toLanguage, string? additionalContext = null)
        => (await TranslateAsync([message], [toLanguage], additionalContext)).FirstOrDefault();

    async Task<string?> TranslateAsync(string text, Language fromLanguage, Language toLanguage, string? additionalContext = null)
    {
        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var message = new Content("", fromLanguage, text);
        var result = await TranslateAsync([message], [language], additionalContext);
        return result?.FirstOrDefault()?.Text;
    }

    async Task<bool> CanTranslateAsync(Language fromLanguage, Language toLanguage)
    {
        var from = await GetLanguageAsync(fromLanguage);
        var to = await GetLanguageAsync(toLanguage);
        return from != null && to != null;
    }
}