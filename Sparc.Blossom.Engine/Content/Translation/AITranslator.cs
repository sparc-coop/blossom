#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Sparc.Blossom.Content.OpenAI;
using Sparc.Blossom.Content.Tovik;
using System.Collections.Concurrent;

namespace Sparc.Blossom.Content;

internal abstract class AITranslator(string defaultModel, decimal costPerToken, int priority = 0) : ITranslator
{
    public int Priority { get; } = priority;
    protected string DefaultModel = defaultModel;
    protected decimal CostPerToken = costPerToken;

    public async Task<TextContent> TranslateAsync(TextContent message, TovikTranslationOptions options)
    {
        var question = new TovikTranslationQuestion(message, options);
        var answer = await AskAsync(question);

        var result = new TextContent(message, options.OutputLanguage ?? message.Language, answer.Text!)
        {
            Type = options.Schema?.Name
        };

        return result;
    }

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options)
    {
        var fromLanguages = messages.GroupBy(x => x.Language);
        var batches = TovikTranslator.Batch(messages, 5);

        var translatedMessages = new ConcurrentBag<TextContent>();

        await Parallel.ForEachAsync(batches, async (batch, _) =>
        {
            var safeBatch = batch.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (safeBatch.Count == 0)
                return;

            foreach (var fromLanguage in fromLanguages)
            {
                var question = new TovikTranslationQuestion(safeBatch, options);
                var answer = await AskAsync(question);
                if (answer.Value?.Text == null)
                    continue;

                var translations = new List<TextContent>();
                foreach (var translation in safeBatch.Where(x => answer.Value.Text.Any(y => x.Id.StartsWith(y.Id))))
                    translations.Add(new TextContent(translation, options.OutputLanguage ?? translation.Language, answer.Value.Text.First(y => translation.Id.StartsWith(y.Id)).Text));

                foreach (var translatedMessage in translations)
                {
                    translatedMessage.AddCharge(answer.TokensUsed, CostPerToken, $"{GetType().Name} translation of {translatedMessage.OriginalText} to {translatedMessage.LanguageId}");
                    translatedMessages.Add(translatedMessage);
                }
            }
        });

        return translatedMessages.ToList();
    }

    public abstract Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question);

    public Task<List<Language>> GetLanguagesAsync()
    {
        return Task.FromResult(new List<Language>());
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage) => true;

}
