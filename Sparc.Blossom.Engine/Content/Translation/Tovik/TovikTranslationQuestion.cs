#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Sparc.Blossom.Content.OpenAI;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sparc.Blossom.Content.Tovik;

internal class TovikTranslationQuestion : OpenAIQuestion<TovikTranslations>
{
    static readonly JsonSerializerOptions TranslateAllUnicode = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    public TovikTranslationQuestion(IEnumerable<TextContent> messages, TovikTranslationOptions options) 
        : base($"")
    {
        Instructions = "You are a translator seeking to accurately translate messages, using the same tone as the provided context, if any. " +
            "If any message is not translatable, use the original message in the output, don't skip it. " +
            "The answer should always contain the same quantity of translations as the input.";
        
        if (options.OutputLanguage?.DialectDisplayName != null && options.OutputLanguage.DialectDisplayName != options.OutputLanguage.LanguageDisplayName)
            Text += $"Translate the following to {options.OutputLanguage.LanguageDisplayName}. " +
                $"Use the {options.OutputLanguage.DialectDisplayName} dialect of {options.OutputLanguage.LanguageDisplayName} when possible:\n\n";
        else if (options.OutputLanguage != null)
            Text += $"Translate the following to {options.OutputLanguage.DisplayName}:\n\n";

        var textToTranslate = messages
            .Where(x => x.Text != null)
            .Select(x => new TovikTranslation(x.Id.Substring(0, 4), x.Text!.Replace('\u00A0', ' ')));

        var messageJson = JsonSerializer.Serialize(textToTranslate, TranslateAllUnicode);
        Text += messageJson;

        if (!string.IsNullOrWhiteSpace(options.AdditionalContext))
            Context.Add(options.AdditionalContext);
    }
}
