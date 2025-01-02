using Microsoft.AspNetCore.Components;
using Sparc.Blossom;

namespace Sparc.Blossom.Realtime;

public class IosPushNotificationService : IPushNotificationService
{
    public Core.Device Device { get; }
    public NavigationManager Nav { get; }

    public IosPushNotificationService(Core.Device device, NavigationManager nav)
    {
        Device = device;
        Nav = nav;
    }

    public void OnNewToken(string token) => Device.PushToken = token;

    public void OnMessageReceived(string url)
    {
        Nav.NavigateTo(url);
    }
}
