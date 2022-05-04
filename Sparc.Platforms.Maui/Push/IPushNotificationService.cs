namespace Sparc.Platforms.Maui;

public interface IPushNotificationService
{
    void OnNewToken(string token);
    void OnMessageReceived(string url);

}
