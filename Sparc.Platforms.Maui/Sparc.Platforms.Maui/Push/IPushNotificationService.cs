namespace Sparc.Platforms.Maui;

public interface IPushNotificationService
{
    string DeviceId { get; }
    PushTokenManager TokenManager { get; }
}
