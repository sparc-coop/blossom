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

        return Languages.OrderBy(x => x.DisplayName).ToList();
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
        foreach (var translator in Translators)
        {
            var languages = await translator.GetLanguagesAsync();
            if (languages.Any(x => x.Id == fromLanguage.Id) && languages.Any(x => x.Id == toLanguage.Id))
                return translator;
        }

        throw new Exception($"No translator found for {fromLanguage.Id} to {toLanguage.Id}");
    }

    public void SetLanguage(BlossomUser user, string? acceptLanguageHeaders)
    {
        if (Languages == null || user.Avatar.Language != null || string.IsNullOrWhiteSpace(acceptLanguageHeaders))
            return;

        // Split the header by comma, then by semicolon to get language codes
        var languages = acceptLanguageHeaders!
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return;

        // Try to find a matching language in LanguagesSpoken or create a new one
        foreach (var langCode in languages)
        {
            // Try to match by Id or DialectId
            var match = Languages.FirstOrDefault(l => l.Matches(langCode));

            if (match != null)
            {
                user.ChangeLanguage(match);
                return;
            }
        }
    }

    internal static Language? GetLanguage(ClaimsPrincipal user, string? fallbackLanguageId = null)
    {
        if (Languages == null)
            return null;

        var languageClaim = user.FindFirst(x => x.Type == "language")?.Value;
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
        return KoriTranslator.GetLanguage(user, fallbackLanguageId);
    }
}