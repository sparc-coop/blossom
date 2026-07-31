using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;
using Google.GenAI;
using Google.GenAI.Types;

namespace Sparc.Blossom.Content;

internal class GoogleTranslator(BlossomEvents channels, Client client) 
    : AITranslator(channels, "", 0.20m / 1_000_000, 0.80m / 1_000_000, 4, 0)
{
    public override async Task VectorizeAsync(IVectorizable message, IEnumerable<IVectorizable>? additionalContext = null)
    {
        await VectorizeAsync([message]);
    }

    public override async Task VectorizeAsync(IEnumerable<IVectorizable> messages, int? lastX = null, int? lookback = null)
    {
        var model = "gemini-embedding-2"; // or "gemini-embedding-2-exp-11-2025"
        var contents = new List<Google.GenAI.Types.Content>();
        var messagesToProcess = messages.Where(x => !string.IsNullOrWhiteSpace(x.Vector.Text) && x.Vector.Text.Contains(';')).ToList();
        if (messagesToProcess.Count == 0)
            throw new Exception("No valid messages to process.");

        foreach (var message in messagesToProcess)
        { 
            var parts = new List<Part>
            {
                new() {
                    FileData = new FileData
                    {
                        MimeType = message.Vector.Text!.Split(";").FirstOrDefault()?.Trim(),
                        FileUri = message.Vector.Text.Split(";").LastOrDefault()?.Trim()
                    }
                }
            };

            contents.Add(new Google.GenAI.Types.Content
            {
                Parts = parts
            });
        }

        var config = new EmbedContentConfig
        {
            OutputDimensionality = 1536
        };

        var output = await client.Models.EmbedContentAsync(model, contents, config);
        foreach (var message in messagesToProcess)
        {
            var values = output.Embeddings?[messagesToProcess.IndexOf(message)].Values;
            if (values != null)
                message.Vector = new(model, values);
        }
    }

    public override async Task<BlossomAnswer<T>> AskAsync<T>(BlossomQuestion<T> question)
    {
        throw new NotImplementedException();
    }
}
