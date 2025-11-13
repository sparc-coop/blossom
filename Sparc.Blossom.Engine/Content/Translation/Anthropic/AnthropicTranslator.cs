using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Sparc.Blossom.Content.OpenAI;
using Sparc.Blossom.Content.Tovik;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Sparc.Blossom.Content;

internal class AnthropicTranslator(AnthropicClient client) : ITranslator
{
    readonly string _defaultModel = AnthropicModels.Claude35Haiku;
    decimal CostPerToken = 5m / 1_000_000;

    public int Priority => 0;

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
        var batches = TovikTranslator.Batch(messages, 10);

        var translatedMessages = new ConcurrentBag<TextContent>();
        await Parallel.ForEachAsync(batches, async (batch, _) =>
        {
            var question = new TovikTranslationQuestion(batch, options);
            var answer = await AskAsync(question);
            foreach (var translation in batch.Where(x => answer.Value != null && answer.Value!.Text.Any(y => x.Id.StartsWith(y.Id))))
            {
                var textContent = new TextContent(translation, options.OutputLanguage ?? translation.Language, answer.Value!.Text.First(y => translation.Id.StartsWith(y.Id)).Text);
                textContent.AddCharge(answer.TokensUsed, CostPerToken, $"Anthropic translation of {textContent.OriginalText} to {textContent.LanguageId}");
                translatedMessages.Add(textContent);
            }
        });
        
        return translatedMessages.ToList();
    }

    private async Task<BlossomAnswer<TovikTranslations>> AskAsync(BlossomQuestion question)
    {
        var answer = new BlossomAnswer<TovikTranslations>();

        try
        {
            var options = CreateResponseOptions(question);
            answer.Log("Info", $"Asking {_defaultModel} {question.Text}");

            var now = DateTime.UtcNow;
            options.Messages = [new(RoleType.User, question.Instructions), new(RoleType.User, question.PromptText)];
            var response = await client.Messages.GetClaudeMessageAsync(options);
            var content = response.Content.OfType<ToolUseContent>().FirstOrDefault()?.Input.ToJsonString();

            var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
            answer.Log("Info", $"Answer {response.Id} in {timeTook}ms: {content}");

            //if (response.Kind == ResponseContentPartKind.Refusal)
            //    answer.SetError(content.Refusal, response.Value.Usage.TotalTokenCount);
            //else if (response.Value.Error != null)
            //{
            //    if (attempt < _maxRetries)
            //    {
            //        answer.Log("Warning", $"Retryable error occurred: {response.Value.Error.Message}. Retrying... Attempt {attempt}/{_maxRetries}");
            //        continue;
            //    }

            //    answer.SetError(response.Value.Error.Message, response.Value.Usage.TotalTokenCount);
            //}
            //else
            answer.SetResponse(response.Id, content.ToString(), response.Usage.OutputTokens);

            return answer;
        }

        catch (Exception ex)
        {
            answer.Log("Error", $"Error occurred: {ex.Message}.");
            answer.SetError(ex.Message);
        }

        return answer;
    }

    public record AnthropicInputSchema(BlossomSchema input_schema);
    private MessageParameters CreateResponseOptions(BlossomQuestion question)
    {
        Console.WriteLine("using schema: " + question.Schema?.ToString());
        var options = new MessageParameters()
        {
            Temperature = 0.2m,
            Model = _defaultModel,
            Stream = false,
            MaxTokens = 1024
        };

        if (question.Schema != null)
        {
            var tool = new Function("structured_output", "Return JSON corresponding strictly to the supplied input schema.",  question.Schema.ToString());
            options.ToolChoice = new() { Type = ToolChoiceType.Tool, Name = "structured_output" };
            options.Tools = [tool];
        }

        return options;
    }

    public Task<List<Language>> GetLanguagesAsync()
    {
        return Task.FromResult(new List<Language>());
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage) => true;
}
