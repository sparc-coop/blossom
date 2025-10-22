using System.Text;

namespace Sparc.Blossom.Content.Tovik;

public class TovikTranslationOptions
{
    public Language? OutputLanguage { get; set; }
    public decimal SlangOrProper { get; set; } = 0.5M;
    public decimal CasualOrFormal { get; set; } = 0.5M;
    public decimal FunnyOrSerious { get; set; } = 0.5M;
    public decimal IrreverentOrRespectful { get; set; } = 0.5M;
    public decimal EnthusiasticOrMatterOfFact { get; set; } = 0.5M;
    public string? Instructions { get; set; }
    public string? AdditionalContext { get; set; }
    public BlossomSchema? Schema { get; set; }

    public string ToPrompt()
    {
        var prompt = new StringBuilder();
        if (Schema != null)
            prompt.AppendLine("Extract data from the following text into the supplied JSON schema.");
        else if (IsDefaultTone)
            prompt.AppendLine($"Translate the following text, using the following rules:");
        else
            prompt.AppendLine($"Translate and update the tone of the following text, using the following rules:");

        if (OutputLanguage != null)
            prompt.AppendLine($"- Translate each item into {OutputLanguage.LanguageDisplayName}.");

        if (OutputLanguage?.DialectDisplayName != null && OutputLanguage.DialectDisplayName != OutputLanguage.LanguageDisplayName)
            prompt.AppendLine($"- Use the {OutputLanguage.DialectDisplayName} dialect of {OutputLanguage.LanguageDisplayName} when possible.");

        if (SlangOrProper != 0.5M)
            prompt.AppendLine("- " + SlangOrProperMappings[Round(SlangOrProper)]);

        if (CasualOrFormal != 0.5M)
            prompt.AppendLine("- " + CasualOrFormalMappings[Round(CasualOrFormal)]);

        if (FunnyOrSerious != 0.5M)
            prompt.AppendLine("- " + FunnyOrSeriousMappings[Round(FunnyOrSerious)]);

        if (IrreverentOrRespectful != 0.5M)
            prompt.AppendLine("- " + IrreverentOrRespectfulMappings[Round(IrreverentOrRespectful)]);

        if (EnthusiasticOrMatterOfFact != 0.5M)
            prompt.AppendLine("- " + EnthusiasticOrMatterOfFactMappings[Round(EnthusiasticOrMatterOfFact)]);

        return prompt.ToString();
    }

    bool IsDefaultTone => SlangOrProper == 0.5M
        && CasualOrFormal == 0.5M
        && FunnyOrSerious == 0.5M
        && IrreverentOrRespectful == 0.5M
        && EnthusiasticOrMatterOfFact == 0.5M;

    static decimal Round(decimal value) => Math.Round(value * 10) / 10;

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
