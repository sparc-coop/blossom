using System.Collections.Concurrent;
using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class BlossomTranslator(IEnumerable<ITranslator> translators, IRepository<TextContent> content)
{
    internal static List<Language>? Languages;

    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<TextContent> Content { get; } = content;

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (Languages == null)
        {
            Languages = [];
            foreach (var translator in Translators.OrderBy(x => x.Priority))
            {
                var languages = await translator.GetLanguagesAsync();
                Languages.AddRange(languages.Where(x => !Languages.Any(y => y.Id.Equals(x.Id, StringComparison.CurrentCultureIgnoreCase))));
            }
        }

        return Languages.OrderBy(x => x.DisplayName).ToList();
    }

    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    async Task<Language?> GetLanguageAsync(Language language) => await GetLanguageAsync(language.Id);

    async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, Language toLanguage, string? additionalContext = null)
    {
        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");
        
        return await TranslateAsync(messages, [language], additionalContext);
    }

    public async Task<TextContent?> TranslateAsync(TextContent message, Language toLanguage, string? additionalContext = null)
        => (await TranslateAsync([message], [toLanguage], additionalContext)).FirstOrDefault();

    async Task<string?> TranslateAsync(string text, Language fromLanguage, Language toLanguage, string? additionalContext = null)
    {
        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var message = new TextContent("", fromLanguage, text);
        var result = await TranslateAsync([message], [language], additionalContext);
        return result?.FirstOrDefault()?.Text;
    }

    internal async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, List<Language> toLanguages, string? additionalContext = null)
    {
        var translatedMessages = new ConcurrentBag<TextContent>();

        var tasks = messages.SelectMany(message =>
            toLanguages.Select(async toLanguage =>
            {
                var translator = await GetBestTranslatorAsync(message.Language, toLanguage);
                var translatedMessage = await translator.TranslateAsync([message], [toLanguage], additionalContext);
                translatedMessages.Add(translatedMessage.First());
            })
        );

        await Task.WhenAll(tasks);
        return translatedMessages.ToList();
    }

    internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (fromLanguage == toLanguage)
            return text;

        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var from = await GetLanguageAsync(fromLanguage);
        var message = new TextContent("", from!, text);
        var result = await TranslateAsync([message], [language]);
        return result?.FirstOrDefault()?.Text;
    }

    async Task<ITranslator> GetBestTranslatorAsync(Language fromLanguage, Language toLanguage)
    {
        foreach (var translator in Translators)
        {
            var languages = await translator.GetLanguagesAsync();
            if (languages.Any(x => x.Id == fromLanguage.Id) && languages.Any(x => x.Id == toLanguage.Id))
                return translator;
        }

        throw new Exception($"No translator found for {fromLanguage.Id} to {toLanguage.Id}");
    }


    internal static Language? GetLanguage(ClaimsPrincipal user, string? fallbackLanguageId = null)
    {
        if (Languages == null)
            return null;

        var languageClaim = user.FindFirst(x => x.Type == ClaimTypes.Locality)?.Value;
        if (string.IsNullOrEmpty(languageClaim))
            return null;

        var language = languageClaim.Split(",")
            .Select(x => x.Split(";").First().Trim())
            .Select(id => new Language(id))
            .Select(lang => Languages.FirstOrDefault(y => y.Id.Equals(lang.Id, StringComparison.CurrentCultureIgnoreCase)))
            .FirstOrDefault(x => x != null);

        language ??= Languages.FirstOrDefault(x => x.Id.Equals(fallbackLanguageId, StringComparison.CurrentCultureIgnoreCase));

        return language;
    }
}

public static class LanguageExtensions
{
    public static Language? Language(this ClaimsPrincipal user, string? fallbackLanguageId = null)
    {
        return BlossomTranslator.GetLanguage(user, fallbackLanguageId);
    }
}