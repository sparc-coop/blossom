using System.Globalization;

namespace Sparc.Engine;

public class Dialect
{
    public string Language { get; set; }
    public string Locale { get; set; }
    public string DisplayName { get; set; }
    public string NativeName { get; set; }
    public List<Voice> Voices { get; set; }

    public Dialect()
    {
        Language = string.Empty;
        Locale = string.Empty;
        DisplayName = string.Empty;
        NativeName = string.Empty;
        Voices = [];
    }

    public Dialect(string localeName)
    {
        var info = CultureInfo.GetCultureInfo(localeName);

        Language = localeName.Split('-').First();
        Locale = localeName.Split('-').Last();
        DisplayName = info.DisplayName;
        NativeName = info.NativeName;
        Voices = [];
    }

    public void AddVoice(Voice voice)
    {
        var existing = Voices.FindIndex(x => x.ShortName == voice.ShortName);

        if (existing == -1)
            Voices.Add(voice);
        else
            Voices[existing] = voice;
    }
}
