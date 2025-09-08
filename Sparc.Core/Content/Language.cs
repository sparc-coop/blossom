using System.Globalization;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public record Language
{
    public string Id { get; set; } = "";
    public string LanguageId { get; set; } = "";
    public string? DialectId { get; set; }
    public string? VoiceId { get; set; }
    public string DisplayName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string LanguageDisplayName { get; set; } = "";
    public string LanguageNativeName { get; set; } = "";
    public string? DialectDisplayName { get; set; }
    public string? DialectNativeName { get; set; }
    public bool? IsRightToLeft { get; set; }

    public Language() {}

    [JsonConstructor]
    public Language(string id)
    {
        Id = id;
        LanguageId = id.Split('-').First();
        DisplayName = "";
        NativeName = "";

        CalculateNames();
    }

    public Language(string id, string displayName, string nativeName, bool? isRightToLeft) : this(id)
    {
        DisplayName = displayName;
        NativeName = nativeName;
        IsRightToLeft = isRightToLeft;

        CalculateNames();
    }

    public static Language FromCulture(string id)
    {
        var language = new Language(id);

        try
        {
            var culture = new CultureInfo(id);
            language.DisplayName = culture.DisplayName;
            language.NativeName = culture.NativeName;
            language.IsRightToLeft = culture.TextInfo.IsRightToLeft;
            language.CalculateNames();
            
        }
        catch (CultureNotFoundException)
        {
            // If the culture is not found, we keep the default values
        }
        return language;
    }

    public override string ToString()
    {
        return LanguageId + (DialectId != null ? "-" + DialectId : "");
    }

    private void CalculateNames()
    {
        if (Id.Contains('-'))
        {
            var elements = Id.Split('-');
            DialectId = string.Join("-", elements.Skip(1));
            LanguageDisplayName = DisplayName.Split('(').First().Trim();
            LanguageNativeName = NativeName.Split('(').First().Trim();
            if (DisplayName.Contains('('))
            {
                DialectDisplayName = DisplayName.Split('(').Last().Trim(')', ' ');
                DialectNativeName = NativeName.Split('(').Last().Trim(')', ' ');
            }
        }
    }

    public bool Matches(Language language)
    {
        if (!LanguageId.Equals(language.LanguageId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (language.DialectId == null)
            return DialectId == null;

        return DialectId?.Equals(language.DialectId, StringComparison.OrdinalIgnoreCase) == true;
    }

    public bool Matches(string langCode)
    {
        var elements = langCode.Split('-');

        if (elements.Length == 1)
            return LanguageId.Equals(langCode, StringComparison.OrdinalIgnoreCase);

        if (elements.Length >= 2)
        {
            var dialectId = string.Join("-", elements.Skip(1));
            return LanguageId.Equals(elements[0], StringComparison.OrdinalIgnoreCase) &&
                    (DialectId == null || DialectId.Equals(dialectId, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public static List<Language> All = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(c => FromCulture(c.Name))
        .OrderBy(l => l.DisplayName)
        .ThenBy(x => x.DialectId == null ? 1 : 0)
        .ToList();

    private static List<string> GoodRandomLanguages = [
        "es-ES", "fr-FR", "de-DE", "it-IT", "ja-JP", "pt-BR", 
        "ko-KR", "nl-NL", "sv-SE", "fi-FI", "no-NO", "da-DK",
        "pl-PL"
    ];
    public static Language Random => All
        .Where(x => GoodRandomLanguages.Contains(x.Id))
        .OrderBy(x => Guid.NewGuid())
        .First();


    public static Language? Find(string? languageClaim)
    {
        if (string.IsNullOrWhiteSpace(languageClaim))
            return null;

        var languages = Language.IdsFrom(languageClaim);
        // Try to find a matching language in LanguagesSpoken or create a new one
        foreach (var langCode in languages)
        {
            // Try to match by Id or DialectId
            var match = All
                .OrderBy(x => x.DialectId != null ? 0 : 1)
                .FirstOrDefault(l => l.Matches(langCode));

            if (match != null)
                return match;
        }

        return null;
    }

    public static List<string> IdsFrom(string? languageClaim)
    {
        if (string.IsNullOrWhiteSpace(languageClaim))
            return [];

        var languages = languageClaim!
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return [];

        return languages;
    }
}

