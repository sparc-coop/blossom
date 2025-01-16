using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Security.Claims;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public class BlossomLocalizer(ClaimsPrincipal principal) : IBlossomLocalizer
{
    public CultureInfo Culture { get; } = principal.Culture();

    public Dictionary<CultureInfo, Dictionary<string, BlossomString>> Translations { get; } = new()
    {
        { principal.Culture(), new() }
    };

    public LocalizedString this[string name]
    {
        get
        {
            if (!Translations.TryGetValue(Culture, out var translations))
            {
                translations = [];
                Translations.Add(Culture, translations);
            }

            if (!translations.TryGetValue(name, out var value))
            {
                value = new(Culture, name, name);
                translations.Add(name, value);
            }

            return value;
        }
    }

    public LocalizedString this[string name, params object[] arguments] => this[name];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return Translations.Values.SelectMany(x => x.Values);
    }
}
