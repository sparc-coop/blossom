#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI;
using OpenAI.Responses;
using Sparc.Blossom.Spaces;
using System.Text;

namespace Sparc.Blossom.Content;

internal class OpenAITranslator(OpenAIClient client) 
    : AITranslator("gpt-4.1-nano", 0.40m / 1_000_000, 0)
{
    public override async Task VectorizeAsync(IVectorizable message, IEnumerable<IVectorizable>? additionalContext = null)
    {
        var model = "text-embedding-3-small";
        var embeddings = client.GetEmbeddingClient(model);

        var messageWithContext = MessagesWithContext(message.Vector.Text, additionalContext?.ToList() ?? [], 1000, 1000);
        var output = await embeddings.GenerateEmbeddingAsync(messageWithContext);
        message.Vector.Vector = output.Value.ToFloats().ToArray();
    }

    public override async Task VectorizeAsync(IEnumerable<IVectorizable> messages, int? lastX = null, int? lookback = null)
    {
        var model = "text-embedding-3-small";
        var embeddings = client.GetEmbeddingClient(model);

        if (!lastX.HasValue)
        {
            var output = await embeddings.GenerateEmbeddingsAsync(messages.Select(x => x.Vector.Text));
            var index = 0;
            foreach (var embedding in output.Value)
            {
                var item = messages.ElementAt(index);
                item.Vector.Vector = embedding.ToFloats().ToArray();
            }
        }

        var batchSize = 1000;
        var offset = 0;
        var vectors = new List<BlossomVector>();
        do
        {
            var batch = messages
                .Where(x => !string.IsNullOrWhiteSpace(x.Vector.Text))
                //.OrderBy(x => x.Sequence)
                .Skip(offset)
                .Take(batchSize)
                .ToList();

            var inputs = batch.Select((x, i) => MessagesWithContext(x.Vector.Text, batch, lookback ?? 0, i))
                .TakeLast(lastX ?? 0)
                .ToList();

            if (!inputs.Any())
                return;

            var outputs = await embeddings.GenerateEmbeddingsAsync(inputs);

            foreach (var output in outputs.Value)
            {
                var vec = batch.ElementAt(output.Index);
                vec.Vector.Vector = output.ToFloats().ToArray();
            }

            offset += batchSize;
        } while (offset < messages.Count());
    }

    private static string MessagesWithContext(string? text, List<IVectorizable> batch, int lookback, int i)
    {
        var previousMessages = batch.Index().Where(x => x.Index < i).Select(x => x.Item.Vector.Text).TakeLast(lookback);
        var context = previousMessages.Any() ? "<PreviousMessages>" + string.Join("\n", previousMessages) + "</PreviousMessages>\n\n" : "";
        return context + text;
    }

    public override async Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question)
    {
        var answer = new BlossomAnswer<T>();

        try
        {
            var options = CreateResponseOptions(question);
            answer.Log("Info", $"Asking {DefaultModel} {question.Text}" + (options.PreviousResponseId == null ? "" : $" from {options.PreviousResponseId}"));

            var now = DateTime.UtcNow;
            var responder = client.GetResponsesClient(DefaultModel);
            
            var response = await responder.CreateResponseAsync(options);
            var message = response.Value.OutputItems.OfType<MessageResponseItem>().First();
            var content = message.Content.First();

            var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
            answer.Log("Info", $"Answer {response.Value.Id} in {timeTook}ms: {content.Text}");

            if (content.Kind == ResponseContentPartKind.Refusal)
                answer.SetError(content.Refusal, response.Value.Usage.TotalTokenCount);
            else if (response.Value.Error != null)
                answer.SetError(response.Value.Error.Message, response.Value.Usage.TotalTokenCount);
            else
                answer.SetResponse(response.Value.Id, content.Text, response.Value.Usage.TotalTokenCount);

            return answer;
        }
        catch (Exception ex)
        {
            answer.Log("Error", $"Error occurred: {ex.Message}.");
            answer.SetError(ex.Message);
        }

        return answer;
    }

    private CreateResponseOptions CreateResponseOptions(BlossomQuestion question)
    {
        List<ResponseItem> prompt = [ResponseItem.CreateUserMessageItem(question.PromptText)];

        var options = new CreateResponseOptions(prompt)
        {
            Temperature = DefaultModel.Contains("4.1") ? 0.2f : null,
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

        if (DefaultModel.Contains('5'))
        {
            options.ReasoningOptions = new()
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Minimal
            };
        }

        return options;
    }

    static BinaryData SchemaToBinary(BlossomSchema schema)
    {
        var lines = schema.ToString().Split('\n');
        var filteredLines = lines.Where(line => !line.Contains(": null")).ToArray();
        var filteredJson = string.Join('\n', filteredLines);

        return BinaryData.FromString(filteredJson);
    }

    static string ToVariableName(string name)
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
}
