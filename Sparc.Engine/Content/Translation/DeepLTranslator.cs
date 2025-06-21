using DeepL;
using DeepL.Model;

namespace Sparc.Engine;

internal class DeepLTranslator(IConfiguration configuration) : ITranslator
{
    Translator? Client;

    internal static List<Language> SourceLanguages = [];
    internal static List<Language> TargetLanguages = [];

    public int Priority => 1;

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
    {
        Client ??= new(configuration.GetConnectionString("DeepL")!);

        var options = new TextTranslateOptions
        {
            SentenceSplittingMode = SentenceSplittingMode.Off,
            Context = additionalContext
        };

        var fromLanguages = messages.GroupBy(x => SourceLanguage(x.Language));
        var toDeepLLanguages = toLanguages.Select(TargetLanguage).Where(x => x != null).ToList();

        var translatedMessages = new List<TextContent>();
        foreach (var sourceLanguage in fromLanguages)
        {
            foreach (var targetLanguage in toDeepLLanguages)
            {
                var texts = messages.Select(x => x.Text).Where(x => x != null);
                var result = await Client.TranslateTextAsync(texts!, sourceLanguage.Key.ToString(), targetLanguage.ToString(), options);
                var newContent = messages.Zip(result, (message, translation) => new TextContent(message, targetLanguage, translation.Text));
                translatedMessages.AddRange(newContent);
            }
        }

        return translatedMessages;
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage)
    {
        return SourceLanguages.Any(x => fromLanguage.Matches(x.Id)) == true &&
               TargetLanguages.Any(x => toLanguage.Matches(x.Id)) == true;
    }

    Language SourceLanguage(Language language)
    {
        return SourceLanguages!
            .OrderBy(x => x.DialectId == null ? 1 : 0)
            .First(x => x.Matches(language.Id));
    }

    Language TargetLanguage(Language language)
    {
        return TargetLanguages!
            .OrderBy(x => x.DialectId == null ? 1 : 0)
            .First(x => x.Matches(language.Id));
    }

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (SourceLanguages.Count > 0 && TargetLanguages.Count > 0)
            return SourceLanguages.Union(TargetLanguages).ToList();

        Client ??= new(configuration.GetConnectionString("DeepL")!);
        
        var sources = await Client.GetSourceLanguagesAsync();
        var targets = await Client.GetTargetLanguagesAsync();

        SourceLanguages = sources
            .Select(x => new Language(x.Code, x.Name, x.Name, x.CultureInfo.TextInfo.IsRightToLeft))
            .ToList();

        TargetLanguages = targets
            .Select(x => new Language(x.Code, x.Name, x.Name, x.CultureInfo.TextInfo.IsRightToLeft))
            .ToList();

        return SourceLanguages.Union(TargetLanguages).ToList();
    }
}