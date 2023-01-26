using Android.App;
using Android.Content;
using Firebase.Messaging;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AndroidPushNotificationService : FirebaseMessagingService, IPushNotificationService
{
    public IDevice Device { get; }
    public NavigationManager Nav { get; }

    public AndroidPushNotificationService()
    {

    }

    public AndroidPushNotificationService(IDevice device, NavigationManager nav)
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
