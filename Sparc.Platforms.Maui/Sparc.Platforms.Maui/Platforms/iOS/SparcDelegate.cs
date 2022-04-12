using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Sparc.Core;
using Sparc.Platforms.Maui.Platforms.iOS.Push;
using System.Threading.Tasks;
using UIKit;
using UserNotifications;

namespace Sparc.Platforms.Maui.Platforms.iOS;

public abstract class SparcDelegate : MauiUIApplicationDelegate
{
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (Platform.OpenUrl(app, url, options))
            return true;

        return base.OpenUrl(app, url, options);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (Platform.ContinueUserActivity(application, userActivity, completionHandler))
            return true;
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    public bool IsPushNotificationEnabled => 
        UIDevice.CurrentDevice.CheckSystemVersion(13, 0) 
        && Services.GetService(typeof(IPushNotificationService)) != null;

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        if (IsPushNotificationEnabled)
        {
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert
                | UNAuthorizationOptions.Badge
                | UNAuthorizationOptions.Sound, (approvalGranted, error) =>
                {
                    if (approvalGranted && error == null)
                        RegisterForRemoteNotifications();
                });
        }

        return base.FinishedLaunching(application, launchOptions);
    }

    static void RegisterForRemoteNotifications()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert |
                UIUserNotificationType.Badge |
                UIUserNotificationType.Sound,
                new NSSet());

            UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
            UIApplication.SharedApplication.RegisterForRemoteNotifications();
        });
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => CompleteRegistration(deviceToken);


    void CompleteRegistration(NSData deviceToken)
    {
        var device = (Device)Services.GetService(typeof(Device));
        if (device != null)
            device.PushToken = deviceToken.ToHexString();
    }
}
