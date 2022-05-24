namespace Sparc.Blossom;

public interface IPushNotificationService
{
    void OnNewToken(string token);
    void OnMessageReceived(string url);

}
