using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom;

public class WindowsPushNotificationService : IPushNotificationService
{
    public Core.Device Device { get; }
    public NavigationManager Nav { get; }

    public WindowsPushNotificationService(Core.Device device, NavigationManager nav)
    {
        Device = device;
        Nav = nav;
    }

    public void OnMessageReceived(string url)
    {
        throw new NotImplementedException();
    }

    public void OnNewToken(string token)
    {
        throw new NotImplementedException();
    }
}
