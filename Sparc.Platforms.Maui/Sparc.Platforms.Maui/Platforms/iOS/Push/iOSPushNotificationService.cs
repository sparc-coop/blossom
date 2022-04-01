using Microsoft.AspNetCore.Components;
using UIKit;

namespace Sparc.Platforms.Maui.Platforms.iOS;

public class iOSPushNotificationService : IPushNotificationService
{
    public PushTokenManager TokenManager { get; }
    public NavigationManager Nav { get; }

    public string DeviceId => UIDevice.CurrentDevice.IdentifierForVendor.ToString();

    public iOSPushNotificationService(PushTokenManager tokenManager, NavigationManager nav)
    {
        TokenManager = tokenManager;
        Nav = nav;
    }

    public void OnNewToken(string token) => TokenManager.UpdateTokenAsync(token).Wait();
}
