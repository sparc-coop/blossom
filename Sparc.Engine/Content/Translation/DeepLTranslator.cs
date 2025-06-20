using DeepL;
using DeepL.Model;

namespace Sparc.Engine;

internal class DeepLTranslator(IConfiguration configuration) : ITranslator
{
    readonly DeepL.Translator Client = new(configuration.GetConnectionString("DeepL")!);

    internal static SourceLanguage[]? SourceLanguages;
    internal static TargetLanguage[]? TargetLanguages;

    public int Priority => 1;

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
    {
        var options = new TextTranslateOptions
        {
            SentenceSplittingMode = SentenceSplittingMode.Off,
            Context = additionalContext
        };

        var fromLanguages = messages.GroupBy(x => x.Language);
        var translatedMessages = new List<TextContent>();
        foreach (var sourceLanguage in fromLanguages)
        {
            foreach (var targetLanguage in toLanguages)
            {
                var texts = messages.Select(x => x.Text).Where(x => x != null);
                var result = await Client.TranslateTextAsync(texts!, sourceLanguage.Key.Id, targetLanguage.Id, options);
                var newContent = messages.Zip(result, (message, translation) => new TextContent(message, targetLanguage, translation.Text));
                translatedMessages.AddRange(newContent);
            }
        }

        return translatedMessages;
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage)
    {
        return SourceLanguages?.Any(x => fromLanguage.Matches(x.Code)) == true &&
               TargetLanguages?.Any(x => toLanguage.Matches(x.Code)) == true;
    }

    public async Task<List<Language>> GetLanguagesAsync()
    {
        SourceLanguages ??= await Client.GetSourceLanguagesAsync();
        TargetLanguages ??= await Client.GetTargetLanguagesAsync();

        var allLanguages =  
            SourceLanguages.Select(x => new Language(x.Code, x.Name, x.Name, x.CultureInfo.TextInfo.IsRightToLeft))
            .Union(
                TargetLanguages.Select(x => new Language(x.Code, x.Name, x.Name, x.CultureInfo.TextInfo.IsRightToLeft)))
            .ToList();

        return allLanguages;
    }
}