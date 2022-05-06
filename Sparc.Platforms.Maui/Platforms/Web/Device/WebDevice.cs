using Blazored.LocalStorage;

namespace Sparc.Platforms.Maui;

public class WebDevice : Core.Device
{
    public WebDevice(ISyncLocalStorageService localStorage)
    {
        LocalStorage = localStorage;
    }

    private string _id;
    public override string Id
    {
        get
        {
            if (_id != null)
                return _id;

            try
            {

                if (LocalStorage.ContainKey("sparc-device-id"))
                    _id = LocalStorage.GetItemAsString("sparc-device-id");
                else
                {
                    _id = Guid.NewGuid().ToString();
                    LocalStorage.SetItemAsString("sparc-device-id", _id);
                }
            }
            catch { }

            return _id;
        }
        set {  _id = value; }
    }

    private string _pushToken;
    public override string PushToken
    {
        get
        {
            if (_pushToken != null) return _pushToken;
            if (LocalStorage.ContainKey("sparc-device-pushtoken"))
                _pushToken = LocalStorage.GetItemAsString("sparc-device-pushtoken");
            return _pushToken;
        }
        set
        {
            _pushToken = value;
            LocalStorage.SetItemAsString("sparc-device-pushtoken", value);
        }
    }

    public override string Platform => Core.Platforms.Web;
    public ISyncLocalStorageService LocalStorage { get; }
}
