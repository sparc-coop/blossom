using Sparc.Platforms.Maui.Platforms.Android;
using System;

namespace Sparc.Platforms.Maui
{
    public interface IPushNotificationService
    {
        PushTokenManager TokenManager { get; }
    }
}