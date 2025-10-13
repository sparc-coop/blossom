#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI;
using OpenAI.Responses;
using Sparc.Blossom.Content.OpenAI;
using Sparc.Blossom.Content.Tovik;
using System.Collections.Concurrent;
using System.Text;

namespace Sparc.Blossom.Content;

internal class OpenAITranslator(OpenAIClient client) : ITranslator
{
    private readonly string _defaultModel = "gpt-4.1-nano";
    private readonly int _maxRetries = 3;
    private readonly int _timeoutSeconds = 30;
    decimal CostPerToken = 0.40m / 1_000_000;

    public int Priority => 0;

    public async Task<TextContent> TranslateAsync(TextContent message, TovikTranslationOptions options)
    {
        var question = new TovikTranslationQuestion(message, options);
        var answer = await AskOpenAIAsync(question);

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

        // TODO: Refactor to use TovikTranslations again when able
        //await Parallel.ForEachAsync(batches, async (batch, _) =>
        //{
        //    var safeBatch = batch.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
        //    if (safeBatch.Count == 0)
        //        return;

        //    foreach (var fromLanguage in fromLanguages)
        //    {
        //            var question = new TovikTranslationQuestion(safeBatch, options);
        //            var answer = await AskOpenAIAsync(question);
        //            if (answer.Value?.Text == null)
        //                continue;

        //            var translations = new List<TextContent>();
        //            foreach (var translation in safeBatch.Where(x => answer.Value.Text.Any(y => x.Id.StartsWith(y.Id))))
        //                translations.Add(new TextContent(translation, options.OutputLanguage ?? translation.Language, answer.Value.Text.First(y => translation.Id.StartsWith(y.Id)).Text));

        //            foreach (var translatedMessage in translations)
        //            {
        //                translatedMessage.AddCharge(answer.TokensUsed, CostPerToken, $"OpenAI translation of {translatedMessage.OriginalText} to {translatedMessage.LanguageId}");
        //                translatedMessages.Add(translatedMessage);
        //            }
        //    }
        //});

        return translatedMessages.ToList();
    }

    private async Task<OpenAIAnswer> AskOpenAIAsync(OpenAIQuestion question)
    { 
        int attempt = 0;
        var answer = new OpenAIAnswer();

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

    private ResponseCreationOptions CreateResponseOptions(OpenAIQuestion question)
    {
        Console.WriteLine("using schema: " + question.Schema?.ToString());
        var options = new ResponseCreationOptions()
        {
            Temperature = 0.2f,
            ServiceTier = new ResponseServiceTier("priority"),
            Instructions = question.Instructions,
            PreviousResponseId = question.PreviousResponseId,
            TextOptions = new()
            {
                TextFormat = question.Schema != null
                    ? ResponseTextFormat.CreateJsonSchemaFormat(ToVariableName(question.Schema!.Name), SchemaToBinary(question.Schema), jsonSchemaIsStrict: true)
                    : ResponseTextFormat.CreateTextFormat()
            }
        };

        return options;
    }

    BinaryData SchemaToBinary(BlossomSchema schema)
    {
        var lines = schema.ToString().Split('\n');
        var filteredLines = lines.Where(line => !line.Contains(": null")).ToArray();
        var filteredJson = string.Join('\n', filteredLines);

        return BinaryData.FromString(filteredJson);
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
