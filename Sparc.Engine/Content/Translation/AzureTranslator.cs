using Azure;
using Azure.AI.Translation.Text;

namespace Sparc.Engine;

internal class AzureTranslator(IConfiguration configuration) : ITranslator
{
    TextTranslationClient? Client;

    internal static List<Language>? Languages;

    public int Priority => 2;

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
    {
        var translatedMessages = new List<TextContent>();
        var azureLanguages = toLanguages.Select(AzureLanguage).Where(x => x != null).ToList();

        var batches = Batch(azureLanguages, 10);

        await ConnectAsync();
        foreach (var batch in batches)
        {
            var options = new TextTranslationTranslateOptions(
                targetLanguages: batch.Select(x => x.Id),
                content: messages.Select(x => x.Text));

            var response = await Client!.TranslateAsync(options);
            var translations = messages.Zip(response.Value);

            foreach (var (sourceContent, result) in translations)
            {
                var newContent = result.Translations.Select(translation =>
                    new TextContent(sourceContent, toLanguages.First(x => x.Matches(translation.TargetLanguage)), translation.Text));
                translatedMessages.AddRange(newContent);
            }
        }

        return translatedMessages;
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage)
    {
        return Languages?.Any(x => x.Matches(fromLanguage) || x.Matches(toLanguage)) == true;
    }

    public Language? AzureLanguage(Language language)
    {
        return Languages?.FirstOrDefault(x => x.Matches(language.Id));
    }

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (Languages != null)
            return Languages;

        await ConnectAsync();
        var languages = await Client!.GetSupportedLanguagesAsync();

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

    private Task ConnectAsync()
    {
         Client ??= new(new AzureKeyCredential(configuration.GetConnectionString("Cognitive")!),
            new Uri("https://api.cognitive.microsofttranslator.com"),
            "southcentralus");

        return Task.CompletedTask;
    }
}
