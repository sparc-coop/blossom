using Sparc.Core;
using UIKit;

namespace Sparc.Platforms.Maui;

public class IosDeviceTokenProvider : ITokenProvider
{
    private string _token;
    public string Token
    {
        get
        {
            if (_token != null) return _token;
            try
            {
                _token = UIDevice.CurrentDevice.IdentifierForVendor.ToString();
            }
            catch
            { }

            return _token;
        }
        set { _token = value; }
    }
}
