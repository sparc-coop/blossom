using Microsoft.AspNetCore.Components;
using Sparc.Blossom;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

public class IosPushNotificationService : IPushNotificationService
{
    public IDevice Device { get; }
    public NavigationManager Nav { get; }

    public IosPushNotificationService(IDevice device, NavigationManager nav)
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
