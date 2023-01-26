using UIKit;

namespace Sparc.Blossom.Authentication;

public class IosDevice : IDevice
{
    private string? _id;
    public string? Id
    {
        get
        {
            if (_id != null) return _id;
            try
            {
                _id = UIDevice.CurrentDevice.IdentifierForVendor.ToString();
            }
            catch
            { }

            return _id;
        }
        set { _id = value; }
    }

    private string? _pushToken;
    public string? PushToken
    {
        get
        {
            if (_pushToken != null) return _pushToken;

            _pushToken = SecureStorage.GetAsync("blossom-device-pushtoken").Result;
            return _pushToken;
        }
        set
        {
            _pushToken = value;
            SecureStorage.SetAsync("blossom-device-pushtoken", value).Wait();
        }
    }

    public string? Platform => "iOS";

    public string? DeviceType { get; set; }
    string? IDevice.Platform { get; set; }
    public string? Idiom { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Name { get; set; }
    public string? VersionString { get; set; }
}
