using System.Globalization;

namespace Sparc.Blossom.Authentication;

public class BlossomRegion(RegionInfo region)
{
    public BlossomRegion() : this(new RegionInfo(CultureInfo.CurrentCulture.Name))
    {
    }

    public string Id { get; set; } = region.TwoLetterISORegionName;
    public string NativeName { get; set; } = region.NativeName;
    public string CurrencyNativeName { get; set; } = region.CurrencyNativeName;
}
