using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Sparc.Blossom.Content.OpenAI;

namespace Sparc.Blossom.Content;

internal class AnthropicTranslator(AnthropicClient client)
    : AITranslator(AnthropicModels.Claude35Haiku, 5m / 1_000_000, 50)
{
    public override async Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question)
    {
        var answer = new BlossomAnswer<T>();

        try
        {
            var options = CreateResponseOptions(question);
            answer.Log("Info", $"Asking {DefaultModel} {question.Text}");

            var now = DateTime.UtcNow;
            options.Messages = [new(RoleType.User, question.Instructions), new(RoleType.User, question.PromptText)];
            var response = await client.Messages.GetClaudeMessageAsync(options);
            var content = response.Content.OfType<ToolUseContent>().FirstOrDefault()?.Input.ToJsonString();

            var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
            answer.Log("Info", $"Answer {response.Id} in {timeTook}ms: {content}");
            answer.SetResponse(response.Id, content!.ToString(), response.Usage.OutputTokens);

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
            Model = DefaultModel,
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
}
