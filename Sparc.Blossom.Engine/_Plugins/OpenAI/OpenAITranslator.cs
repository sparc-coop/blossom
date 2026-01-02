#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI;
using OpenAI.Responses;
using System.Text;

namespace Sparc.Blossom.Content;

internal class OpenAITranslator(OpenAIClient client) 
    : AITranslator("gpt-4.1-nano", 0.40m / 1_000_000, 0)
{
    public override async Task<IEnumerable<BlossomVector>> VectorizeAsync(IEnumerable<TextContent> messages)
    {
        var model = "text-embedding-3-small";
        var embeddings = client.GetEmbeddingClient(model);

        var batchSize = 1000;
        var offset = 0;
        var vectors = new List<BlossomVector>();
        do
        {
            var batch = messages
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                //.OrderBy(x => x.Sequence)
                .Skip(offset)
                .OrderBy(x => x.Timestamp)
                .Take(batchSize)
                .ToList();

            var inputs = batch.Select((x, i) => MessagesWithContext(x.Text, batch, i)).ToList();

            if (!inputs.Any())
                return [];

            var outputs = await embeddings.GenerateEmbeddingsAsync(inputs);

            foreach (var output in outputs.Value)
            {
                var text = batch.ElementAt(output.Index);
                vectors.Add(new(text.SpaceId, text.Id, model, output.ToFloats().ToArray()));
            }

            offset += batchSize;
        } while (offset < messages.Count());

        return vectors;
    }

    private static string MessagesWithContext(string? text, List<TextContent> batch, int i)
    {
        var previousMessages = batch.Index().Where(x => x.Index < i).Select(x => x.Item.Text).TakeLast(5);
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
