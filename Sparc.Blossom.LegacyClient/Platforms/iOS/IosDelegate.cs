using Foundation;
using Sparc.Blossom.Realtime;
using UIKit;
using UserNotifications;

namespace Sparc.Blossom;

public abstract class BlossomDelegate : MauiUIApplicationDelegate
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
        UIDevice.CurrentDevice.CheckSystemVersion(10, 0) &&
        Services.GetService(typeof(IPushNotificationService)) != null;

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
                        UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
        }

        return base.FinishedLaunching(application, launchOptions);
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => CompleteRegistrationAsync(deviceToken).ContinueWith((task) => { if (task.IsFaulted) throw task.Exception; });


    Task CompleteRegistrationAsync(NSData deviceToken)
    {
        var device = (Core.Device)Services.GetService(typeof(Core.Device));
        if (device != null)
            device.PushToken = deviceToken.ToHexString();

        return Task.CompletedTask;
    }
}
