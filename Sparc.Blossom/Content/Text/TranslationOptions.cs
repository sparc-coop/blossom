using Sparc.Blossom.Authentication;
using System.Text;

namespace Sparc.Blossom.Content;

public record Tones(
    decimal SlangOrProper = 0.5M,
    decimal CasualOrFormal = 0.5M,
    decimal FunnyOrSerious = 0.5M,
    decimal IrreverentOrRespectful = 0.5M,
    decimal EnthusiasticOrMatterOfFact = 0.5M
);

public class TranslationOptions
{
    public TranslationOptions()
    { }

    public TranslationOptions(Language outputLanguage)
    {
        OutputLanguage = outputLanguage;
    }

    public Language? OutputLanguage { get; set; }
    public TovikSettings TovikSettings { get; set; } = new(1, []);
    public Tones? Tone { get; set; }
    public string? Instructions { get; set; }
    public string? AdditionalContext { get; set; }
    public string? WindowedContext { get; set; }
    public BlossomSchema? Schema { get; set; }
    public bool RunInBackground { get; set; }
    public string? BackgroundId { get; set; }
    public bool CrawlHtml { get; set; }

    public string ToPrompt()
    {
        var prompt = new StringBuilder();
        var context = WindowedContext ?? AdditionalContext;

        if (!string.IsNullOrWhiteSpace(context))
        {
            prompt.AppendLine("Given the following context:");
            prompt.AppendLine(context.Substring(0, Math.Min(context.Length, 1000)));
            prompt.AppendLine().AppendLine();
        }


        if (Schema != null)
            prompt.AppendLine("Extract data from the following text into the supplied JSON schema.");
        else if (Tone == null)
            prompt.AppendLine($"Translate the following text, using the following rules:");
        else
            prompt.AppendLine($"Translate and update the tone of the following text, using the following rules:");

        if (OutputLanguage != null)
            prompt.AppendLine($"- Translate each item into {OutputLanguage.LanguageDisplayName}.");

        if (OutputLanguage?.DialectDisplayName != null && OutputLanguage.DialectDisplayName != OutputLanguage.LanguageDisplayName)
            prompt.AppendLine($"- Use the {OutputLanguage.DialectDisplayName} dialect of {OutputLanguage.LanguageDisplayName} when possible.");

        if (Tone != null)
        {
            if (Tone.SlangOrProper != 0.5M)
                prompt.AppendLine("- " + SlangOrProperMappings[Round(Tone.SlangOrProper)]);

            if (Tone.CasualOrFormal != 0.5M)
                prompt.AppendLine("- " + CasualOrFormalMappings[Round(Tone.CasualOrFormal)]);

            if (Tone.FunnyOrSerious != 0.5M)
                prompt.AppendLine("- " + FunnyOrSeriousMappings[Round(Tone.FunnyOrSerious)]);

            if (Tone.IrreverentOrRespectful != 0.5M)
                prompt.AppendLine("- " + IrreverentOrRespectfulMappings[Round(Tone.IrreverentOrRespectful)]);

            if (Tone.EnthusiasticOrMatterOfFact != 0.5M)
                prompt.AppendLine("- " + EnthusiasticOrMatterOfFactMappings[Round(Tone.EnthusiasticOrMatterOfFact)]);
        }

        if (TovikSettings.IgnoreList != null && TovikSettings.IgnoreList.Count > 0)
        {
            prompt.AppendLine("- Never translate the following words and phrases (if transliteration is needed for these, do so):");
            foreach (var item in TovikSettings.IgnoreList)
                prompt.AppendLine($"  - {item}");
        }

        prompt.AppendLine().AppendLine();

        return prompt.ToString();
    }

    static decimal Round(decimal value) => Math.Round(value * 10) / 10;

    public void SetWindowedContext(List<TextContent> content, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(AdditionalContext))
            AdditionalContext = string.Join(" ", content.Select(x => x.Text));

        if (AdditionalContext.Length <= maxChars)
        {
            WindowedContext = AdditionalContext;
            return;
        }

        var firstItemIndex = AdditionalContext.IndexOf(content.First().Text ?? "");
        var lastItemIndex = AdditionalContext.LastIndexOf(content.Last().Text ?? "");
        var numSamples = firstItemIndex > -1 && lastItemIndex > -1
            ? lastItemIndex - firstItemIndex > maxChars ? 2 : 1
            : firstItemIndex > -1 || lastItemIndex > -1 ? 1
                : 0;

        if (numSamples == 0)
            WindowedContext = AdditionalContext.Substring(0, maxChars);
        else if (numSamples == 2)
        {
            var firstStartIndex = Math.Max(0, firstItemIndex - maxChars / 4);
            var firstEndIndex = Math.Min(AdditionalContext.Length, firstItemIndex + maxChars / 4);
            var lastStartIndex = Math.Max(0, lastItemIndex - maxChars / 4);
            var lastEndIndex = Math.Min(AdditionalContext.Length, lastItemIndex + maxChars / 4);
            WindowedContext = AdditionalContext.Substring(firstStartIndex, firstEndIndex - firstStartIndex) 
                + AdditionalContext.Substring(lastStartIndex, lastEndIndex - lastStartIndex);
        }
        else
        {
            var index = firstItemIndex > -1 ? firstItemIndex : lastItemIndex;
            var start = Math.Max(0, index - maxChars / 2);
            var end = Math.Min(AdditionalContext.Length, index + maxChars / 2);

            // ensure we get as close to totalChars as possible
            if (end - start < maxChars)
            {
                start = Math.Max(0, end - maxChars);
                end = Math.Min(AdditionalContext.Length, start + maxChars);
            }

            WindowedContext = AdditionalContext.Substring(start, end - start);
        }
    }

    static Dictionary<decimal, string> SlangOrProperMappings = new()
    {
        { 0.0M, "Use heavy slang, contractions, and colloquial shortcuts; grammar may be loose." },
        { 0.1M, "Mostly slang with some standard grammar; casual shortcuts common." },
        { 0.2M, "Slang is frequent, but grammar is improving; informal tone." },
        { 0.3M, "Noticeable slang, but sentences are more structured." },
        { 0.4M, "Some slang, but mostly standard grammar." },
        { 0.5M, "Balanced mix of slang and proper grammar." },
        { 0.6M, "Mostly proper grammar, with occasional slang." },
        { 0.7M, "Proper grammar is dominant; rare slang." },
        { 0.8M, "Formal grammar, almost no slang." },
        { 0.9M, "Strict grammar, no slang; formal tone." },
        { 1.0M, "Use proper grammar, full sentences, and formal language throughout." }
    };

    static Dictionary<decimal, string> CasualOrFormalMappings = new()
    {
        { 0.0M, "Extremely casual; friendly, relaxed, and informal." },
        { 0.1M, "Very casual; conversational and easygoing." },
        { 0.2M, "Casual; informal but polite." },
        { 0.3M, "Somewhat casual; relaxed but with some structure." },
        { 0.4M, "Neutral-casual; approachable but not overly informal." },
        { 0.5M, "Balanced; neither casual nor formal." },
        { 0.6M, "Slightly formal; polite and structured." },
        { 0.7M, "Formal; respectful and professional." },
        { 0.8M, "Very formal; highly structured and polite." },
        { 0.9M, "Extremely formal; ceremonial and official." },
        { 1.0M, "Strictly formal; use official language and etiquette." }
    };

    static Dictionary<decimal, string> FunnyOrSeriousMappings = new()
    {
        { 0.0M, "Extremely humorous; jokes and playful language throughout." },
        { 0.1M, "Very funny; lighthearted and witty." },
        { 0.2M, "Funny; casual humor and puns." },
        { 0.3M, "Somewhat funny; occasional jokes." },
        { 0.4M, "Neutral-funny; friendly with a touch of humor." },
        { 0.5M, "Balanced; neither funny nor serious." },
        { 0.6M, "Slightly serious; mostly factual, some light humor." },
        { 0.7M, "Serious; focus on facts, little humor." },
        { 0.8M, "Very serious; strictly factual and direct." },
        { 0.9M, "Extremely serious; no humor, only facts." },
        { 1.0M, "Completely serious; formal and factual throughout." }
    };

    static Dictionary<decimal, string> IrreverentOrRespectfulMappings = new()
    {
        { 0.0M, "Extremely irreverent; disregard for conventions and authority." },
        { 0.1M, "Very irreverent; playful and cheeky." },
        { 0.2M, "Irreverent; casual and bold." },
        { 0.3M, "Somewhat irreverent; informal and direct." },
        { 0.4M, "Neutral-irreverent; relaxed but not disrespectful." },
        { 0.5M, "Balanced; neither irreverent nor respectful." },
        { 0.6M, "Slightly respectful; polite and considerate." },
        { 0.7M, "Respectful; formal and deferential." },
        { 0.8M, "Very respectful; highly polite and considerate." },
        { 0.9M, "Extremely respectful; ceremonial and deferential." },
        { 1.0M, "Completely respectful; utmost politeness and formality." }
    };

    static Dictionary<decimal, string> EnthusiasticOrMatterOfFactMappings = new()
    {
        { 0.0M, "Extremely enthusiastic; energetic and passionate language." },
        { 0.1M, "Very enthusiastic; lively and excited tone." },
        { 0.2M, "Enthusiastic; positive and upbeat." },
        { 0.3M, "Somewhat enthusiastic; friendly and encouraging." },
        { 0.4M, "Neutral-enthusiastic; approachable but not overly excited." },
        { 0.5M, "Balanced; neither enthusiastic nor matter-of-fact." },
        { 0.6M, "Slightly matter-of-fact; calm and direct." },
        { 0.7M, "Matter-of-fact; straightforward and neutral." },
        { 0.8M, "Very matter-of-fact; strictly factual and unemotional." },
        { 0.9M, "Extremely matter-of-fact; dry and impersonal." },
        { 1.0M, "Completely matter-of-fact; only facts, no emotion." }
    };
}
