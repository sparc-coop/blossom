using Android.App;
using Android.Content;
using Firebase.Messaging;
using Microsoft.AspNetCore.Components;
using Sparc.Core;

namespace Sparc.Platforms.Maui;

[Service]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AndroidPushNotificationService : FirebaseMessagingService, IPushNotificationService
{
    public Device Device { get; }
    public NavigationManager Nav { get; }

    public AndroidPushNotificationService(Device device, NavigationManager nav)
    {
        Device = device;
        Nav = nav;
    }

    public override void OnNewToken(string token) => Device.PushToken = token;

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
