namespace Sparc.Blossom.Content.OpenAI;

internal class OpenAIQuestion<T>(string text)
{
    public List<string> Context { get; set; } = [];
    public string Text { get; set; } = text;
    public string? PreviousResponseId { get; set; }
    public string? Instructions { get; set; }
    public OpenAISchema Schema { get; set; } = new(typeof(T));

    public string? PromptText => Context.Count != 0
            ? $"Given the following context:\n{ContextText}\n\nQuestion: {Text}"
            : Text;

    public string ContextText =>
        PreviousResponseId == null
        ? string.Join("\n", Context.Where(x => !string.IsNullOrWhiteSpace(x)))
        : "";
}
