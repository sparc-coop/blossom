using DeepL;
using DeepL.Model;

namespace Kori;

internal class DeepLTranslator(IConfiguration configuration) : ITranslator
{
    readonly DeepL.Translator Client = new(configuration.GetConnectionString("DeepL")!);

    internal static SourceLanguage[]? Languages;

    public int Priority => 1;

    public async Task<List<Content>> TranslateAsync(IEnumerable<Content> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
    {
        var options = new TextTranslateOptions
        {
            SentenceSplittingMode = SentenceSplittingMode.Off,
            Context = additionalContext
        };

        var fromLanguages = messages.GroupBy(x => x.Language);
        var translatedMessages = new List<Content>();
        foreach (var sourceLanguage in fromLanguages)
        {
            foreach (var targetLanguage in toLanguages)
            {
                var texts = messages.Select(x => x.Text).Where(x => x != null);
                var result = await Client.TranslateTextAsync(texts!, sourceLanguage.Key.Id, targetLanguage.Id, options);
                var newContent = messages.Zip(result, (message, translation) => new Content(message, targetLanguage, translation.Text));
                translatedMessages.AddRange(newContent);
            }
        }

        return translatedMessages;
    }
   
    public async Task<List<Language>> GetLanguagesAsync()
    {
        Languages ??= await Client.GetSourceLanguagesAsync();
        return Languages
            .Select(x => new Language(x.Code, x.Name, x.Name, x.CultureInfo.TextInfo.IsRightToLeft))
            .ToList();
    }
}