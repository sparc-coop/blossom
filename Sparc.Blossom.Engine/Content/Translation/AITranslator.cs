using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;
using System.Collections.Concurrent;

namespace Sparc.Blossom.Content;

record ContentTranslated(TextContent TranslatedContent) : BlossomEvent;
internal abstract class AITranslator(BlossomEvents channels, string defaultModel, decimal inputCostPerToken, decimal outputCostPerToken, int priority = 0) : ITranslator
{
    public int Priority { get; } = priority;
    protected string DefaultModel = defaultModel;
    protected decimal InputCostPerToken = inputCostPerToken;
    protected decimal OutputCostPerToken = outputCostPerToken;

    public abstract Task VectorizeAsync(IVectorizable item, IEnumerable<IVectorizable>? additionalContext = null);
    public abstract Task VectorizeAsync(IEnumerable<IVectorizable> items, int? lastX = null, int? lookback = null);

    internal async Task IntersectAsync(List<BlossomSpace> spaces)
    {
        foreach (var space in spaces)
        {
            var question = new SummaryQuestion(space, spaces.Except([space]));
            var answer = await AskAsync(question);
            space.SetSummary(answer.Value);
        }
    }

    public async Task<List<TextContent>> TranslateAsync(TranslationRequest request)
    {
        var fromLanguages = request.Content.GroupBy(x => x.Language);
        var batches = request.Content.Batch(10);

        var translatedMessages = new ConcurrentBag<TextContent>();

        var now = DateTime.UtcNow;
        await Parallel.ForEachAsync(batches, async (batch, _) =>
        {
            var safeBatch = batch.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (safeBatch.Count == 0)
                return;

            request.Options.SetWindowedContext(safeBatch, 1000);
            
            foreach (var fromLanguage in fromLanguages)
            {
                var question = new TranslationQuestion(safeBatch, request.Options);
                var answer = await AskAsync(question);
                if (answer.Value?.Text == null)
                    continue;

                var translations = new List<TextContent>();
                foreach (var translation in safeBatch.Where(x => answer.Value.Text.Any(y => x.Id.StartsWith(y.Id))))
                    translations.Add(new TextContent(translation, request.Options.OutputLanguage ?? translation.Language, answer.Value.Text.First(y => translation.Id.StartsWith(y.Id)).Text));

                foreach (var translatedMessage in translations)
                {
                    if (request.Options.BackgroundId != null)
                        await channels.Publish(request.Options.BackgroundId, new ContentTranslated(translatedMessage));
                    
                    translatedMessage.AddCharge(answer.InputTokens, InputCostPerToken, answer.OutputTokens, OutputCostPerToken, $"{GetType().Name} translation of {translatedMessage.OriginalText} to {translatedMessage.LanguageId}");
                    translatedMessages.Add(translatedMessage);
                }
            }
        });

        var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
        Console.WriteLine("*** Translated {0} messages in {1} ms", translatedMessages.Count, timeTook);

        var result = translatedMessages.ToList();
        result.ForEach(x => x.Version = request.Options.TovikSettings.Version);

        return result;
    }

    public abstract Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question);

    public Task<List<Language>> GetLanguagesAsync()
    {
        return Task.FromResult(new List<Language>());
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage) => true;
}
