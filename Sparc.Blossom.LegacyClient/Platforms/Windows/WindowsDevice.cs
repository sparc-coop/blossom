using Blazored.LocalStorage;

namespace Sparc.Blossom;

public class WindowsDevice : Core.Device
{
    public WindowsDevice(ISyncLocalStorageService localStorage)
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

                if (LocalStorage.ContainKey("blossom-device-id"))
                    _id = LocalStorage.GetItemAsString("blossom-device-id");
                else
                {
                    _id = Guid.NewGuid().ToString();
                    LocalStorage.SetItemAsString("blossom-device-id", _id);
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
            if (LocalStorage.ContainKey("blossom-device-pushtoken"))
                _pushToken = LocalStorage.GetItemAsString("blossom-device-pushtoken");
            return _pushToken;
        }
        set
        {
            _pushToken = value;
            LocalStorage.SetItemAsString("blossom-device-pushtoken", value);
        }
    }

    public override string Platform => "Windows";
    public ISyncLocalStorageService LocalStorage { get; }
}
