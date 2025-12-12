using System.ComponentModel;

namespace Sparc.Blossom.Content;

public class TranslationResult
{
    [Description("The original given ID of the original text as Id, and the translated text in the target language as Text.")]
    public List<TextContentBase> Text { get; set; } = [];
}

public record TextContentBase(string Id, string Text);