#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel;

namespace Sparc.Blossom.Content.Tovik;

public class TovikTranslations
{
    [Description("The original given ID of the original text as Id, and the translated text in the target language as Text.")]
    public List<TovikTranslation> Text { get; set; } = [];
}

public record TovikTranslation(string Id, string Text);