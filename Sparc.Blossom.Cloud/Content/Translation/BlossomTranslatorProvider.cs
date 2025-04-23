using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class BlossomTranslatorProvider(IEnumerable<ITranslator> translators, IRepository<Content> content)
{
    internal static List<Language>? Languages;

    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<Content> Content { get; } = content;

    internal async Task<List<Language>> GetLanguagesAsync()
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

        return Languages;
    }

    //internal async Task<List<Content>> TranslateAsync(Content message, string toLanguage)
    //{
    //    var language = await GetLanguageAsync(toLanguage)
    //        ?? throw new ArgumentException($"Language {toLanguage} not found");
    //    return await TranslateAsync(message, [language]);
    //}

    //internal async Task<List<Content>> TranslateAsync(IEnumerable<Content> messages, List<Language> toLanguages)
    //{
    //    var processedLanguages = new List<Language>(toLanguages);
    //    var messages = new List<Content>();
    //    foreach (var translator in Translators)
    //    {
    //        var languages = await translator.GetLanguagesAsync();
    //        if (!languages.Any(x => x.Id.ToUpper() == message.Language.ToUpper()))
    //            continue;

    //        try
    //        {
    //            var languagesToTranslate = processedLanguages.Where(x => languages.Any(y => y.Id.ToUpper() == x.Id.ToUpper())).ToList();
    //            messages.AddRange(await translator.TranslateAsync(message, languagesToTranslate));
    //            processedLanguages.RemoveAll(x => languagesToTranslate.Any(y => y.Id.ToUpper() == x.Id.ToUpper()));
    //            if (!processedLanguages.Any())
    //                break;
    //        }
    //        catch
    //        {
    //            continue;
    //        }
    //    }

    //    return messages;
    //}

    //internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    //{
    //    if (fromLanguage == toLanguage)
    //        return text;

    //    var language = await GetLanguageAsync(toLanguage)
    //        ?? throw new ArgumentException($"Language {toLanguage} not found");

    //    var message = new Content("", fromLanguage, text);
    //    var result = await TranslateAsync([message], [language]);
    //    return result?.FirstOrDefault()?.Text;
    //}

    public async Task<ITranslator?> For(Language fromLanguage, Language toLanguage)
    {
        foreach (var translator in Translators)
        {
            if (await translator.CanTranslateAsync(fromLanguage, toLanguage))
                return translator;
        }

        return null;
    }

    public async Task<ITranslator?> For(Content originalContent, Language toLanguage)
        => await For(originalContent.Language, toLanguage);

    internal static Language? GetLanguage(ClaimsPrincipal user, string? fallbackLanguageId = null)
    {
        if (Languages == null)
            return null;

        var languageClaim = user.FindFirstValue(ClaimTypes.Locality);
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
        return BlossomTranslatorProvider.GetLanguage(user, fallbackLanguageId);
    }
}