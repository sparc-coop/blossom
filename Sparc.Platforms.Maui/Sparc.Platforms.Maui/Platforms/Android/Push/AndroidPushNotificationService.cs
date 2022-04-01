using Android.App;
using Android.Content;
using Android.Provider;
using Firebase.Messaging;
using Microsoft.AspNetCore.Components;

namespace Sparc.Platforms.Maui.Platforms.Android;

[Service]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AndroidPushNotificationService : FirebaseMessagingService, IPushNotificationService
{
    public PushTokenManager TokenManager { get; }
    public NavigationManager Nav { get; }

    public string DeviceId => Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);

    public AndroidPushNotificationService(NavigationManager nav)
    {
        Nav = nav;
    }

    public override void OnNewToken(string token) => TokenManager.UpdateTokenAsync(token).Wait();

    public override void OnMessageReceived(RemoteMessage message)
    {
        if (message.Data.TryGetValue("action", out var url))
            Nav.NavigateTo(url);
    }
}
