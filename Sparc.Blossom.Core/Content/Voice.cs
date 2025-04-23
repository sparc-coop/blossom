namespace Sparc.Blossom;

public record Voice(string Locale, string Name, string DisplayName, string LocaleName, string ShortName, string Gender, string VoiceType);
public record Word(long Offset, long Duration, string Text);
