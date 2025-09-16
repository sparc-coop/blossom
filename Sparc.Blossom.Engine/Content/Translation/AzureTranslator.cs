using Azure;
using Azure.AI.Translation.Text;
using Sparc.Blossom.Content.Tovik;

namespace Sparc.Blossom.Content;

internal class AzureTranslator(IConfiguration configuration) : ITranslator
{
    static TextTranslationClient? Client;

    internal static List<Language>? Languages;

    public int Priority => 2;
    decimal CostPerWord => 10.00m / 1_000_000 * 5; // $10 per million characters, assuming average 5 characters per word

    public async Task<TextContent> TranslateAsync(TextContent message, TovikTranslationOptions options)
    {
        var result = await TranslateAsync([message], options);
        return result.First();
    }

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options)
    {
        var azureLanguage = AzureLanguage(options.OutputLanguage!);

        await ConnectAsync();
        var azureOptions = new TextTranslationTranslateOptions(
            targetLanguages: [options.OutputLanguage!.Id],
            content: messages.Select(x => x.Text));

        var response = await Client!.TranslateAsync(azureOptions);
        var translations = messages.Zip(response.Value);

        var translatedMessages = new List<TextContent>();
        foreach (var (sourceContent, result) in translations)
        {
            var newContent = result.Translations.Select(translation =>
                new TextContent(sourceContent, options.OutputLanguage!, translation.Text));

            translatedMessages.AddRange(newContent);
            translatedMessages.ForEach(x => x.AddCharge(CostPerWord, $"Azure translation of {x.OriginalText} to {x.LanguageId}"));
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

    private Task ConnectAsync()
    {
        Client ??= new(new AzureKeyCredential(configuration.GetConnectionString("Cognitive")!),
           new Uri("https://api.cognitive.microsofttranslator.com"),
           "southcentralus");

        return Task.CompletedTask;
    }
}
