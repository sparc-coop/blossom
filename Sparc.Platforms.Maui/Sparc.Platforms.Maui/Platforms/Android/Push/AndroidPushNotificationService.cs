using Android.App;
using Android.Content;
using Firebase.Messaging;
using Microsoft.AspNetCore.Components;

namespace Sparc.Platforms.Maui;

[Service]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AndroidPushNotificationService : FirebaseMessagingService, IPushNotificationService
{
    public PushTokenProvider TokenManager { get; }
    public NavigationManager Nav { get; }

    public AndroidPushNotificationService(NavigationManager nav)
    {
        Nav = nav;
    }

    public override void OnNewToken(string token) => TokenManager.UpdateTokenAsync(token).Wait();

    public override void OnMessageReceived(RemoteMessage message)
    {
        if (message.Data.TryGetValue("action", out var url))
            OnMessageReceived(url);
    }

    public void OnMessageReceived(string url)
    {
        Nav.NavigateTo(url);
    }
}
