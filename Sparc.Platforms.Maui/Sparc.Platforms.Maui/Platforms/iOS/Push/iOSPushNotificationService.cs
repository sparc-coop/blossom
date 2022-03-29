using Microsoft.AspNetCore.Components;

namespace Sparc.Platforms.Maui.Platforms.Android;

public class iOSPushNotificationService : IPushNotificationService
{
    public PushTokenManager TokenManager { get; }
    public NavigationManager Nav { get; }

    public iOSPushNotificationService(PushTokenManager tokenManager, NavigationManager nav)
    {
        TokenManager = tokenManager;
        Nav = nav;
    }

    public void OnNewToken(string token) => TokenManager.UpdateToken(token);
}
