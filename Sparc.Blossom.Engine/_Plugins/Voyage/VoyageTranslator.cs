
namespace Sparc.Blossom.Content;

internal class VoyageTranslator : AITranslator
{
    HttpClient Client = new() { BaseAddress = new Uri("https://api.voyageai.com/v1/") };

    public VoyageTranslator(IConfiguration config) : base("voyage-4", 0.06m / 1_000_000, 1)
    {
        var apiKey = config.GetConnectionString("Voyage");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public override Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question)
    {
        throw new NotImplementedException();
    }

    record EmbeddingRequest(List<string> input, string model, string? input_type = null, int? output_dimension = null);
    record EmbeddingResponse(List<Embedding> data, string model, EmbeddingUsage usage);
    record Embedding(List<float> embedding, int index);
    record EmbeddingUsage(int total_tokens);
    public override async Task<BlossomVector> VectorizeAsync(TextContent message, IEnumerable<TextContent>? additionalContext = null)
    {
        var result = await VectorizeAsync([message]);
        return result.First();
    }

    public override async Task<IEnumerable<BlossomVector>> VectorizeAsync(IEnumerable<TextContent> messages, int? lastX = null, int? lookback = null)
    {
        var text = messages.Select(m => m.Text).ToList();

        var data = new EmbeddingRequest(text, DefaultModel, null, 1024);
        var output = await Client.PostAsJsonAsync("embeddings", data);
        output.EnsureSuccessStatusCode();

        var response = await output.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return response!.data.Select((output, index) => 
            new BlossomVector(messages.ElementAt(index).SpaceId, messages.ElementAt(index).ContentType, messages.ElementAt(index).Id, output.embedding.ToArray())
        {
            Text = messages.ElementAt(index).Text
        });
    }
}
