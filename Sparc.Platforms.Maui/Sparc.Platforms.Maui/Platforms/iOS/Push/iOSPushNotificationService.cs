using Microsoft.AspNetCore.Components;

namespace Sparc.Platforms.Maui;

public class IosPushNotificationService : IPushNotificationService
{
    public PushTokenProvider TokenProvider { get; }
    public NavigationManager Nav { get; }

    public IosPushNotificationService(PushTokenProvider tokenProvider, NavigationManager nav)
    {
        TokenProvider = tokenProvider;
        Nav = nav;
    }

    public void OnNewToken(string token) => TokenProvider.UpdateTokenAsync(token).Wait();

    public void OnMessageReceived(string url)
    {
        Nav.NavigateTo(url);
    }
}
