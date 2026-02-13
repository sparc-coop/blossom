
using Sparc.Blossom.Spaces;

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

    record EmbeddingRequest(IEnumerable<string> input, string model, string? input_type = null, int? output_dimension = null);
    record EmbeddingResponse(List<Embedding> data, string model, EmbeddingUsage usage);
    record Embedding(List<float> embedding, int index);
    record EmbeddingUsage(int total_tokens);
    public override async Task VectorizeAsync(IVectorizable item, IEnumerable<IVectorizable>? additionalContext = null)
    {
        await VectorizeAsync([item]);
    }

    public override async Task VectorizeAsync(IEnumerable<IVectorizable> items, int? lastX = null, int? lookback = null)
    {
        var itemsToProcess = items.Where(x => !string.IsNullOrWhiteSpace(x.Vector.Text)).ToList();

        var data = new EmbeddingRequest(itemsToProcess.Select(x => x.Vector.Text!), DefaultModel, null, 1024);
        var output = await Client.PostAsJsonAsync("embeddings", data);
        output.EnsureSuccessStatusCode();

        var response = await output.Content.ReadFromJsonAsync<EmbeddingResponse>() 
            ?? throw new Exception("Failed to parse embedding response from Voyage API.");
        
        var index = 0;
        foreach (var embedding in response.data)
        {
            var item = itemsToProcess.ElementAt(index++);
            item.Vector.Vector = embedding.embedding.ToArray();
        }
    }
}
