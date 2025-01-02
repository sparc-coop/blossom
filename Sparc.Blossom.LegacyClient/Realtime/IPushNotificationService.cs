namespace Sparc.Blossom.Realtime;

public interface IPushNotificationService
{
    void OnNewToken(string token);
    void OnMessageReceived(string url);

}
