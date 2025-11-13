namespace Sparc.Blossom.Content.OpenAI;

internal class BlossomQuestion(string text)
{
    public List<string> Context { get; set; } = [];
    public string Text { get; set; } = text;
    public string? PreviousResponseId { get; set; }
    public string? Instructions { get; set; }
    public BlossomSchema? Schema { get; set; }

    public string? PromptText => Context.Count != 0
            ? $"Given the following context:\n{ContextText}\n\nQuestion: {Text}"
            : Text;

    public string ContextText =>
        PreviousResponseId == null
        ? string.Join("\n", Context.Where(x => !string.IsNullOrWhiteSpace(x)))
        : "";
}

internal class BlossomQuestion<T> : BlossomQuestion
{
    public BlossomQuestion(string text) : base(text)
    {
        Schema = new(typeof(T));
    }
}
