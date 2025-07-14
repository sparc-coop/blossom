using System.Globalization;

namespace Sparc.Core.Billing;

public class SparcCurrency(RegionInfo region)
{
    public SparcCurrency() : this(new RegionInfo(CultureInfo.CurrentCulture.Name))
    {
    }

    public string Id { get; set; } = region.ISOCurrencySymbol;
    public string Name { get; set; } = region.CurrencyEnglishName;
    public string Symbol { get; set; } = region.CurrencySymbol;
    public string NativeName { get; set; } = region.CurrencyNativeName;

    public static SparcCurrency From(string currency)
    {
        var matchingRegion = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .FirstOrDefault(region => region.ISOCurrencySymbol.Equals(currency, StringComparison.OrdinalIgnoreCase));

        return matchingRegion != null
            ? new SparcCurrency(matchingRegion)
            : new SparcCurrency();
    }

    public static List<SparcCurrency> All()
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Select(region => new SparcCurrency(region))
            .GroupBy(currency => currency.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    public string ToString(decimal amount, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        var region = new RegionInfo(culture.Name);
        if (region.ISOCurrencySymbol == Id)
            return amount.ToString("C", culture);

        var matchingCulture = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .FirstOrDefault(c => new RegionInfo(c.Name).ISOCurrencySymbol == Id);
        if (matchingCulture != null)
            return amount.ToString("C", matchingCulture);

        return $"{Id} {amount:N0}";
    }
}