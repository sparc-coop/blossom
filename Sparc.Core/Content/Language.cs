
namespace Sparc.Engine;

public record Language
{
    public string LanguageId { get; set; } = "";
    public string? DialectId { get; set; }
    public string? VoiceId { get; set; }
    public string DisplayName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public bool? IsRightToLeft { get; set; }

    public Language() {}

    public Language(string id)
    {
        LanguageId = id.Split('-').First();
        DisplayName = "";
        NativeName = "";

        if (id.Contains('-'))
        {
            DialectId = id.Split('-').Last();
            DisplayName = DisplayName.Split('(').First().Trim();
            NativeName = NativeName.Split('(').First().Trim();
        }
    }

    public Language(string id, string displayName, string nativeName, bool? isRightToLeft) : this(id)
    {
        DisplayName = displayName;
        NativeName = nativeName;
        IsRightToLeft = isRightToLeft;
    }

    public override string ToString()
    {
        return LanguageId + (DialectId != null ? "-" + DialectId : "");
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

        if (elements.Length == 2)
            return LanguageId.Equals(elements[0], StringComparison.OrdinalIgnoreCase) &&
                   DialectId?.Equals(elements[1], StringComparison.OrdinalIgnoreCase) == true;

        return false;
    }
}

