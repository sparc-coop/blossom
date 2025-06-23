using Sparc.Blossom.Authentication;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Sparc.Engine;

public class KoriTranslator(IEnumerable<ITranslator> translators, IRepository<TextContent> content)
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
                Languages.AddRange(languages.Where(x => !Languages.Any(y => y.Matches(x))));
            }
        }

        Languages = Languages.OrderBy(x => x.Id)
            .ThenBy(x => x.DialectId == null ? 1 : 0)
            .ToList();

        return Languages;
    }

    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    public async Task<TextContent?> TranslateAsync(TextContent message, Language toLanguage, string? additionalContext = null)
        => (await TranslateAsync([message], [toLanguage], additionalContext)).FirstOrDefault();

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
        return Translators
            .OrderBy(x => x.Priority)
            .FirstOrDefault(x => x.CanTranslate(fromLanguage, toLanguage))
            ?? throw new Exception($"No translator found for {fromLanguage.Id} to {toLanguage.Id}");
    }

    public void SetLanguage(BlossomUser user, string? acceptLanguageHeaders)
    {
        if (Languages == null || string.IsNullOrWhiteSpace(acceptLanguageHeaders))
            return;

        // Split the header by comma, then by semicolon to get language codes
        var match = GetLanguage(acceptLanguageHeaders);
        if (match != null)
            user.ChangeLanguage(match);
    }

    internal static Language? GetLanguage(string languageClaim)
    {
        if (Languages == null)
            return null;

        var languages = languageClaim
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return null;

        // Try to find a matching language in LanguagesSpoken or create a new one
        foreach (var langCode in languages)
        {
            // Try to match by Id or DialectId
            var match = Languages
                .OrderBy(x => x.DialectId != null ? 0 : 1)
                .FirstOrDefault(l => l.Matches(langCode));

            if (match != null)
                return match;
        }

        return null;
    }
}