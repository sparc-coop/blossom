using DeepL;
using Sparc.Blossom.Content.Tovik;
using System.Linq;

namespace Sparc.Blossom.Content;

internal class DeepLTranslator(IConfiguration configuration) : ITranslator
{
    static Translator? Client;

    internal static List<Language> SourceLanguages = [];
    internal static List<Language> TargetLanguages = [];

    public int Priority => 1;
    decimal CostPerWord => 25.00m / 1_000_000 * 5; // $25 per million characters, assuming average 5 characters per word

    public async Task<TextContent> TranslateAsync(TextContent message, TovikTranslationOptions options)
    {
        var result = await TranslateAsync([message], options);
        return result.First();
    }
    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options)
    {
        Client ??= new(configuration.GetConnectionString("DeepL")!);

        var deepLOptions = new TextTranslateOptions
        {
            Context = options.AdditionalContext,
            ModelType = ModelType.PreferQualityOptimized
        };

        var fromLanguages = messages.GroupBy(x => SourceLanguage(x.Language));
        var toDeepLLanguage = TargetLanguage(options.OutputLanguage!);

        var batches = TovikTranslator.Batch(messages, 50);

        var translatedMessages = new List<TextContent>();
        foreach (var batch in batches)
        {
            var safeBatch = batch.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (safeBatch.Count == 0)
                continue;

            foreach (var sourceLanguage in fromLanguages)
            {
                var safeTargetLanguage = toDeepLLanguage.ToString() == "en" ? "en-US" : toDeepLLanguage.ToString(); // en is deprecated
                var texts = safeBatch.Select(x => x.Text);
                var result = await Client.TranslateTextAsync(texts!, sourceLanguage.Key.ToString(), safeTargetLanguage, deepLOptions);
                var newContent = safeBatch.Zip(result, (message, translation) => new TextContent(message, options.OutputLanguage!, translation.Text));
                translatedMessages.AddRange(newContent);
                translatedMessages.ForEach(x => x.AddCharge(CostPerWord, $"DeepL translation of {x.OriginalText} to {x.LanguageId}"));
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
            .Select(x => Language.FromCulture(x.Code))
            .ToList();

        TargetLanguages = targets
            .Select(x => Language.FromCulture(x.Code))
            .ToList();

        return SourceLanguages.Union(TargetLanguages).ToList();
    }
}