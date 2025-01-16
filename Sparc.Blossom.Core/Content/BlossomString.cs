using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Sparc.Blossom;

public class BlossomString(CultureInfo culture, string name, string value) : LocalizedString(name, value)
{
    public CultureInfo Culture { get; } = culture;
}
