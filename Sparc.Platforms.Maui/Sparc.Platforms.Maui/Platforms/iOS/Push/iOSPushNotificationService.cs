using Microsoft.AspNetCore.Components;
using Sparc.Core;

namespace Sparc.Platforms.Maui;

public class IosPushNotificationService : IPushNotificationService
{
    public Device Device { get; }
    public NavigationManager Nav { get; }

    public IosPushNotificationService(Device device, NavigationManager nav)
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
