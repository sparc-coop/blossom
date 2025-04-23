namespace Sparc.Blossom;

public record Language
{
    public string Id { get; private set; } = "";
    public string? DialectId { get; set; }
    public string? VoiceId { get; set; }
    public string DisplayName { get; private set; } = "";
    public string NativeName { get; private set; } = "";
    public bool? IsRightToLeft { get; private set; }
    public List<Dialect> Dialects { get; private set; } = [];

    public Language() {}

    internal Language(string id)
    {
        Id = id.Split('-').First();
        if (id.Contains('-'))
            DialectId = id.Split('-').Last();

        DisplayName = "";
        NativeName = "";
        Dialects = [];

        if (id.Contains('-'))
        {
            AddDialect(id);
            DisplayName = DisplayName.Split('(').First().Trim();
            NativeName = NativeName.Split('(').First().Trim();
        }
    }

    internal Language(string id, string displayName, string nativeName, bool? isRightToLeft) : this(id)
    {
        DisplayName = displayName;
        NativeName = nativeName;
        IsRightToLeft = isRightToLeft;
    }

    internal void AddDialect(string locale, List<Voice>? voices = null)
    {
        Dialect dialect = new(locale);
        if (voices != null)
            foreach (var voice in voices)
                dialect.AddVoice(voice);

        var existing = Dialects.FindIndex(x => x.Locale == dialect.Locale);

        if (existing == -1)
            Dialects.Add(dialect);
        else
            Dialects[existing] = dialect;
    }

    public override string ToString()
    {
        return Id + (DialectId != null ? "-" + DialectId : "");
    }
}

