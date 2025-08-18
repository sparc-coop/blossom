#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI;
using OpenAI.Responses;
using Sparc.Blossom.Content.OpenAI;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Sparc.Blossom.Content;

public class OpenAITranslations
{
    [Description("The translated text in the target language.")]
    public List<string> Text { get; set; } = [];
}

internal class OpenAITranslationQuestion : OpenAIQuestion<OpenAITranslations>
{
    static readonly JsonSerializerOptions TranslateAllUnicode = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    
    public OpenAITranslationQuestion(IEnumerable<TextContent> messages, Language toLanguage, string? additionalContext = null) 
        : base($"Translate the following to {toLanguage.DisplayName}:\n\n")
    {
        Instructions = "You are a translator seeking to accurately translate messages, using the same tone as the provided context, if any. If any message is not translatable, use the original message in the output, don't skip it.";
        var messageJson = JsonSerializer.Serialize(messages.Select(x => new { x.Text }), TranslateAllUnicode);
        Text += messageJson;

        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            Context.Add($"Here is a sampling of other content on the page:");
            Context.Add(additionalContext);
        }
    }
}

internal class OpenAITranslator(OpenAIClient client) : ITranslator
{
    private readonly string _defaultModel = "gpt-4.1-nano";
    private readonly int _maxRetries = 3;
    private readonly int _timeoutSeconds = 15;
    decimal CostPerToken = 0.40m / 1_000_000;

    public int Priority => 0;

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, IEnumerable<Language> toLanguages, string? additionalContext = null)
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
                foreach (var toLanguage in toLanguages)
                {
                    var question = new OpenAITranslationQuestion(safeBatch, toLanguage, additionalContext);
                    var answer = await AskOpenAIAsync(question);
                    if (answer.Value == null)
                        continue;

                    var translations = safeBatch.Zip(answer.Value.Text,
                        (message, translation) => new TextContent(message, toLanguage, translation)
                        .AddCharge(answer.TokensUsed, CostPerToken, $"OpenAI translation of {message.OriginalText} to {message.LanguageId}"));

                    foreach (var translatedMessage in translations)
                        translatedMessages.Add(translatedMessage);
                }
            }
        });

        return translatedMessages.ToList();
    }

    private async Task<OpenAIAnswer<T>> AskOpenAIAsync<T>(OpenAIQuestion<T> question)
    { 
        int attempt = 0;
        var answer = new OpenAIAnswer<T>();

        while (attempt < _maxRetries)
        {
            try
            {
                attempt++;

                var options = CreateResponseOptions(question);
                //answer.Log("Context", question.ContextText);
                answer.Log("Info", $"Asking {_defaultModel} {question.Text}" + (options.PreviousResponseId == null ? "" : $" from {options.PreviousResponseId}"));

                var now = DateTime.UtcNow;
                var responder = client.GetOpenAIResponseClient(_defaultModel);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
               
                var response = await responder.CreateResponseAsync(question.PromptText, options, cts.Token);
                var message = response.Value.OutputItems.OfType<MessageResponseItem>().First();
                var content = message.Content.First();

                var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
                answer.Log("Info", $"Answer {response.Value.Id} in {timeTook}ms: {content.Text}");

                if (content.Kind == ResponseContentPartKind.Refusal)
                    answer.SetError(content.Refusal, response.Value.Usage.TotalTokenCount);
                else if (response.Value.Error != null)
                {
                    if (attempt < _maxRetries)
                    {
                        answer.Log("Warning", $"Retryable error occurred: {response.Value.Error.Message}. Retrying... Attempt {attempt}/{_maxRetries}");
                        continue;
                    }

                    answer.SetError(response.Value.Error.Message, response.Value.Usage.TotalTokenCount);
                }
                else
                    answer.SetResponse(response.Value.Id, content.Text, response.Value.Usage.TotalTokenCount);

                return answer;
            }
            catch (TaskCanceledException e)
            {
                answer.SetError($"Timeout occurred: {e.Message}. Attempt {attempt}/{_maxRetries}");
            }
            catch (Exception ex)
            {
                answer.Log("Error", $"Error occurred: {ex.Message}. Attempt {attempt}/{_maxRetries}");
                answer.SetError(ex.Message);
            }
        }

        return answer;
    }

    private ResponseCreationOptions CreateResponseOptions<T>(OpenAIQuestion<T> question)
    {
        var options = new ResponseCreationOptions()
        {
            Temperature = 0.2f,
            Instructions = question.Instructions,
            PreviousResponseId = question.PreviousResponseId,
            TextOptions = new()
            {
                TextFormat = question.Schema != null
                    ? ResponseTextFormat.CreateJsonSchemaFormat(ToVariableName(question.Schema!.Name), question.Schema.ToBinary(), jsonSchemaIsStrict: true)
                    : ResponseTextFormat.CreateTextFormat()
            }
        };

        return options;
    }

    private static string ToVariableName(string name)
    {
        var result = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                result.Append(c);
            else
                result.Append('_');
        }
        return result.ToString();
    }

    public Task<List<Language>> GetLanguagesAsync() 
    {
        return Task.FromResult(new List<Language>());
    }

    public bool CanTranslate(Language fromLanguage, Language toLanguage) => true;
}
