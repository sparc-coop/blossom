using Sparc.Core;
using Android.Provider;
using Android.App;

namespace Sparc.Platforms.Maui;

public class AndroidDeviceTokenProvider : ITokenProvider
{
    private string _token;
    public string Token
    {
        get
        {
            if (_token != null) return _token;
            try
            {
                _token = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
            }
            catch
            { }

            return _token;
        }
        set { _token = value; }
    }
}
