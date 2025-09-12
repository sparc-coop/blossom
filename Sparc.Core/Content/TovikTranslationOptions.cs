namespace Sparc.Blossom.Content.Tovik;

public record TovikTranslationOptions(
    Language? OutputLanguage = null, 
    decimal? SlangOrProper = null, 
    decimal? CasualOrFormal = null, 
    decimal? FunnyOrSerious = null, 
    decimal? IrreverentOrRespectful = null, 
    decimal? EnthusiasticOrMatterOfFact = null, 
    string? AdditionalContext = null);
