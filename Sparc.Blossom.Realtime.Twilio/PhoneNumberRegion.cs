using PhoneNumbers;

namespace Sparc.Blossom.Realtime;

public class PhoneNumberRegion
{
    public int CallingCode { get; set; }
    public string CountryCode { get; set; }
    public string CountryName { get; set; }

    public PhoneNumberRegion(int callingCode)
    {
        CallingCode = callingCode;
        CountryCode = PhoneNumberUtil.GetInstance().GetRegionCodeForCountryCode(callingCode);
        CountryName = new System.Globalization.RegionInfo(CountryCode).NativeName;
    }

    public PhoneNumberRegion(string countryCode)
    {
        CountryCode = countryCode;
        CallingCode = PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(countryCode);
        CountryName = new System.Globalization.RegionInfo(countryCode).NativeName;
    }

    public override string ToString()
    {
        return $"+{CountryCode} ({CountryName})";
    }

    public static List<PhoneNumberRegion> GetAll()
    {
        var callingCodes = PhoneNumberUtil.GetInstance().GetSupportedCallingCodes();
        var countryCodes = callingCodes.Select(c => new PhoneNumberRegion(c)).ToList();
        return countryCodes;
    }
}
