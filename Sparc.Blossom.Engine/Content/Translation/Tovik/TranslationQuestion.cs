using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sparc.Blossom.Content;

public record TextContentBase(string Id, string Text);
public class TranslationResult
{
    [Description("The original given ID of the original text as Id, and the translated text in the target language as Text.")]
    public List<TextContentBase> Text { get; set; } = [];
}

internal class TranslationQuestion : BlossomQuestion<TranslationResult>
{
    static readonly JsonSerializerOptions TranslateAllUnicode = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TranslationQuestion(TextContent message, TranslationOptions options)
    : this([message], options)
    {
    }

    public TranslationQuestion(IEnumerable<TextContent> messages, TranslationOptions options)
        : base(options.ToPrompt())
    {
        Instructions = "You are a translation and tone‑shaping assistant.\r\n" +
            "For each message you receive, adjust grammar, vocabulary, idioms, and sentence structure to match the specified tone levels, while preserving the original meaning.\r\n\r\n" +
            "If an output language is also provided, first translate each message into that language, then adjust the grammar, vocabulary, idioms, and sentence structure.\r\n\r\n" +
            "If any message is not translatable, use the original message in the output; do not skip it. The answer must always contain the same number of output messages as the input.\r\n\r\n" +
            "Return exactly one output for each input message, in the same order. Do not omit, merge, split, summarize, or add messages.\r\n\r\n" +
            "If tone levels are not specified but context is provided, match the general tone and formality of the provided context. If required tone or language information is missing or ambiguous, do not guess beyond the provided context; preserve meaning and apply only what is explicitly specified.\r\n\r\n" +
            "The answer should always contain the same quantity of translations as the input.";

        var textToTranslate = messages
            .Where(x => x.Text != null)
            .Select(x => new TextContentBase(x.Id.Substring(0, 4), x.Text!.Replace('\u00A0', ' ')));

        var messageJson = JsonSerializer.Serialize(textToTranslate, TranslateAllUnicode);
        Text += messageJson;

        if (options.Schema != null)
            Schema = options.Schema;
    }
}
