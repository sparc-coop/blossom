using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.Extensions.AI;
using Sparc.Blossom.Content.OpenAI;
using Sparc.Blossom.Content.Tovik;

namespace Sparc.Blossom.Content;

internal class VertexTranslator(IConfiguration config) : AITranslator("tovik-emma", 0, 0)
{
    IChatClient? Client;

    async Task Initialize()
    {
        if (Client != null)
            return;

        var builder = new PredictionServiceClientBuilder
        {
            Endpoint = "https://mg-endpoint-c11aca87-657d-492e-be87-5dbd145d67d2.us-central1-563775580158.prediction.vertexai.goog:443",
            ApiKey = config.GetConnectionString("Google")
        };

        Client = await builder.BuildIChatClientAsync(
            EndpointName.FormatProjectLocationEndpoint("613182263813", "us-central1", "mg-endpoint-c11aca87-657d-492e-be87-5dbd145d67d2"));
    }

    public override async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options)
    {
        options.Instructions = "Return the result as a list of ID and translated text pairs, one pair per line, separated by a colon, like the following example. Do not include any additional text.\r\n\r\n"
            + "asb3: This is the translated text for content with ID asb3.\r\n"
            + "d0kj: This is the translated text for content with ID d0kj.";

        return await base.TranslateAsync(messages, options);
    }

    public override async Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question)
    {
        await Initialize();
        
        var answer = new BlossomAnswer<T>();

        try
        {
            answer.Log("Info", $"Asking {DefaultModel} {question.Text}");

            var now = DateTime.UtcNow;
            var response = await Client!.GetResponseAsync(question.PromptText!);
            var timeTook = (DateTime.UtcNow - now).TotalMilliseconds;
            answer.Log("Info", $"Answer {response.ConversationId} in {timeTook}ms: {response.Text}");

            var result = response.Text.Split("\r\n")
                .Select(line => line.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            TovikTranslations translations = new() { Text = result.Select(kvp => new TovikTranslation(kvp.Key, kvp.Value)).ToList() };

            return answer;
        }
        catch (Exception ex)
        {
            answer.Log("Error", $"Error occurred: {ex.Message}.");
            answer.SetError(ex.Message);
        }

        return answer;

    }
}
