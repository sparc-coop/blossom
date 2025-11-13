using Sparc.Blossom.Content.OpenAI;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sparc.Blossom.Content.Tovik;

internal class TovikTranslationQuestion : BlossomQuestion
{
    static readonly JsonSerializerOptions TranslateAllUnicode = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TovikTranslationQuestion(TextContent message, TovikTranslationOptions options)
    : base(options.ToPrompt())
    {
        Instructions = 
            options.Instructions ?? (
            "You are a translation and tone‑shaping assistant.\r\n" +
            "For the following message, adjust grammar, vocabulary, idioms, and sentence structure to match the specified tone levels, while preserving the original meaning.\r\n\r\n" +
            "If an output language is also provided, first translate the message into that language, then adjust the grammar, vocabulary, idioms, and sentence structure.\r\n\r\n" +
            "If the message is not translatable, use the original message in the output.");

        if (options.Schema != null)
            Schema = options.Schema;

        Text += "\r\n\r\nText to translate: " + message.Text!.Replace('\u00A0', ' ');
    }


    public TovikTranslationQuestion(IEnumerable<TextContent> messages, TovikTranslationOptions options) 
        : base(options.ToPrompt())
    {
        Instructions = "You are a translation and tone‑shaping assistant.\r\n" +
            "For each message you receive, adjust grammar, vocabulary, idioms, and sentence structure to match the specified tone levels, while preserving the original meaning.\r\n\r\n" +
            "If an output language is also provided, first translate each message into that language, then adjust the grammar, vocabulary, idioms, and sentence structure.\r\n\r\n" +
            "If any message is not translatable, use the original message in the output, don't skip it. " +
            "The answer should always contain the same quantity of translations as the input.";
        
        var textToTranslate = messages
            .Where(x => x.Text != null)
            .Select(x => new TovikTranslation(x.Id.Substring(0, 4), x.Text!.Replace('\u00A0', ' ')));

        var messageJson = JsonSerializer.Serialize(textToTranslate, TranslateAllUnicode);
        Text += messageJson;

        if (options.Schema != null)
            Schema = options.Schema;
    }
}
