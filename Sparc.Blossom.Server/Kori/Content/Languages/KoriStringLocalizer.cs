using Microsoft.Extensions.Localization;

namespace Sparc.Blossom.Kori;

public class KoriStringLocalizer : IStringLocalizer
{
    public LocalizedString this[string name] => throw new NotImplementedException();

    public LocalizedString this[string name, params object[] arguments] => throw new NotImplementedException();

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        throw new NotImplementedException();
    }
}
