using Azure;
using Azure.AI.Translation.Text;

namespace Kori;

internal class AzureTranslator(IConfiguration configuration) : ITranslator
{
    readonly TextTranslationClient Client = new(new AzureKeyCredential(configuration.GetConnectionString("Cognitive")!),
            new Uri("https://api.cognitive.microsofttranslator.com"),
            "southcentralus");

    internal static List<Language>? Languages;

    public int Priority => 2;

    public async Task<List<Content>> TranslateAsync(IEnumerable<Content> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
    {
        var translatedMessages = new List<Content>();
        var batches = Batch(toLanguages, 10);
        
        foreach (var batch in batches)
        {
            var options = new TextTranslationTranslateOptions(
                targetLanguages: batch.Select(x => x.Id),
                content: messages.Select(x => x.Text));

            var response = await Client.TranslateAsync(options);
            var translations = messages.Zip(response.Value);

            foreach (var (sourceContent, result) in translations)
            {
                var newContent = result.Translations.Select(translation =>
                    new Content(sourceContent, toLanguages.First(x => x.Id == translation.TargetLanguage), translation.Text));
                translatedMessages.AddRange(newContent);
            }
        }

        return translatedMessages;
    }

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (Languages != null)
            return Languages;

        var languages = await Client.GetSupportedLanguagesAsync();

        Languages = languages.Value.Translation
            .Select(x => new Language(x.Key, x.Value.Name, x.Value.NativeName, x.Value.Directionality == LanguageDirectionality.RightToLeft))
            .ToList();

        return Languages;
    }

    // from https://stackoverflow.com/a/13731854
    internal static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items,
                                                       int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }
}
