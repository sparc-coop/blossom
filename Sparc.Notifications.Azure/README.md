# Sparc.Notifications.Azure

The `Sparc.Notifications.Azure` project enables integration between Sparc projects and Azure Notification Hubs, with fallbacks for Web Push. 

## Getting Started

### Step 1: Install the Package

Install the `Sparc.Notifications.Azure` NuGet package to your *Sparc Features* project:

```cli
> dotnet add package Sparc.Notifications.Azure
```

### Step 2: Set Up Your Platforms

#### Android

1. Create a project in Firebase: https://firebase.google.com/docs/projects/learn-more#project-id
2. Add an Android App to your Firebase project and download the `google-services.json` file, then add it to your *Sparc MAUI* platform project.
3. In Firebase Console, switch to the **Cloud Messaging** tab and copy the Server Key. 
4. Add the following entries to your *Sparc MAUI* .csproj file ([reference](https://github.com/dotnet/maui/issues/5458#issuecomment-1078446136)):
```xml
<ItemGroup>
    <GoogleServicesJson Include="google-services.json" />
</ItemGroup>
```
5. Add the following entries to your *Sparc MAUI* `AndroidManifest.xml` file:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.GET_ACCOUNTS"/>
```
6. 

### Step 3: Set Up Azure Notification Hubs

1. Create a Notification Hub in Azure and copy the `DefaultFullSharedAccessSignature` connection string.
2. In the Google settings, copy the API key from step 3 in the Android section above.
2. In the Apple settings, copy the API key from step 3 in the Apple section above.

### Step 4: Send Messages

## Documentation & Help

See the Github repository link present on this package for more info.

