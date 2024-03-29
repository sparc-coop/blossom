﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.OS;
using Android.Runtime;

namespace Sparc.Blossom;

public class BlossomActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);
        if (IsPlayServicesAvailable())
            CreateNotificationChannel();
    }

    protected override void OnResume()
    {
        base.OnResume();

        Platform.OnResume(this);
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);

        Platform.OnNewIntent(intent);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    public bool IsPlayServicesAvailable()
    {
        int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
        return resultCode == ConnectionResult.Success;
    }

    void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            // Notification channels are new in API 26 (and not a part of the
            // support library). There is no need to create a notification
            // channel on older versions of Android.
            return;
        }

#if ANDROID26_0_OR_GREATER
        var defaultChannel = new NotificationChannel("default",
                                              "Default Notifications",
                                              NotificationImportance.Default)
        {
            Description = "Default messages appear in this channel"
        };

        var urgentChannel = new NotificationChannel("urgent",
                                              "Urgent Notifications",
                                              NotificationImportance.High)
        {
            Description = "Urgent messages appear in this channel"
        };

        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.CreateNotificationChannel(defaultChannel);
        notificationManager.CreateNotificationChannel(urgentChannel);
#endif
    }

}
