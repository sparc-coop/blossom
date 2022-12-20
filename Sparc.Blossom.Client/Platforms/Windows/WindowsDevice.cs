using Blazored.LocalStorage;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public class WindowsDevice : IDevice
{
    public WindowsDevice(ISyncLocalStorageService localStorage)
    {
        LocalStorage = localStorage;
    }

    private string? _id;
    public string Id
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

    private string? _pushToken;
    public string PushToken
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

    public string Platform => "Windows";
    public ISyncLocalStorageService LocalStorage { get; }
    public string? DeviceType { get; set; }
    string? IDevice.Platform { get; set; }
    public string? Idiom { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Name { get; set; }
    public string? VersionString { get; set; }
}
