# Sparc.Notifications.Azure

The `Sparc.Notifications.Azure` library enables integration between Sparc projects and Azure Notification Hubs, with fallbacks for Web Push. 

## Getting Started

### Step 1: Install the Package

1. Install the `Sparc.Notifications.Azure` NuGet package to your *Sparc Features* project:

    ```cli
    > dotnet add package Sparc.Notifications.Azure
    ```

2. Add the following line of code to Startup.cs to register the library's services:

    ```csharp
    services.AddAzurePushNotifications(Configuration.GetSection("Notifications"));
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

#### Web

1. Add a Web Push Key Pair in the Cloud Messaging section of your Firebase project.
2. Add the necessary JavaScript to your *Sparc Web* index.html file ([reference](https://firebase.google.com/docs/cloud-messaging/js/client#web-version-9)). Example:

    ```html
    <body>
      <!-- Insert this script at the bottom of the HTML, but before you use any Firebase services -->
      <script type="module">
        import { initializeApp } from 'https://www.gstatic.com/firebasejs/9.6.10/firebase-app.js'

        // Add Firebase products that you want to use
        import { getMessaging, getToken, onMessage } from 'https://www.gstatic.com/firebasejs/9.6.10/firebase-messaging.js'

        // TODO: Replace the following with your app's Firebase project configuration
        // See: https://firebase.google.com/docs/web/learn-more#config-object
        const firebaseConfig = {
          // ...
        };

        // Initialize Firebase
        const app = initializeApp(firebaseConfig);


        // Initialize Firebase Cloud Messaging and get a reference to the service
        const messaging = getMessaging(app);

        // Get registration token. Initially this makes a network call, once retrieved
        // subsequent calls to getToken will return from cache.
        const messaging = getMessaging();
        getToken(messaging, { vapidKey: '<YOUR_PUBLIC_VAPID_KEY_HERE>' }).then((currentToken) => {
          if (currentToken) {
            // Send the token to your server and update the UI if necessary
            // ...
          } else {
            // Show permission request UI
            console.log('No registration token available. Request permission to generate one.');
            // ...
          }
        }).catch((err) => {
          console.log('An error occurred while retrieving token. ', err);
          // ...
        });

      </script>
    </body>

    ```
3. Add an empty `firebase-messaging-sw.js` file to the root of your domain (ie. the wwwroot folder of your *Sparc Web* project).
4. Set up your app to handle messages that come in when your app is in the foreground (background messages are handled automatically by the browser):

    ```js
    // Handle incoming messages. Called when:
    // - a message is received while the app has focus
    // - the user clicks on an app notification created by a service worker
    //   `messaging.onBackgroundMessage` handler.

    onMessage(messaging, (payload) => {
      console.log('Message received. ', payload);
      // ...
    });
    ```

### Step 3: Set Up Azure Notification Hubs

1. Create a Notification Hub in Azure and copy the `DefaultFullSharedAccessSignature` connection string.
2. In the Google/FCM settings, copy the API key from step 3 in the Android section above.
3. In the Apple settings, copy the API key from step 3 in the Apple section above.
4. Add the following section to your *Sparc Web* / *Sparc MAUI* projects' appsettings.json file:

    ```json
    {
       "Notifications": {
          "ConnectionString": "[The DefaultFullSharedAccessSignature from Step 1 above]",
          "HubName": "[The name of your Azure Hub]"
       }
    }
    ```

### Step 4: Register the User's Devices in the Backend

1. Inject `AzureNotificationService` into an appropriate Feature (i.e. a feature that registers users and/or devices).

    ```csharp
    public RegisterDevice(AzureNotificationService notifications) => Notifications = notifications;
    ```

2. Call the `RegisterAsync` method on the `AzureNotificationService` to register the device with Azure Notification Hubs, passing in the token received from the client.

    ```csharp
    // userId is your app's identifier for the user
    // device is an injectable object of type Device that automatically pulls device and push token info from each Sparc platform 

    await notifications.RegisterAsync(userId, device);
    ```

### Step 5: Send Messages

1. Inject `AzureNotificationService` into the Feature.

    ```csharp
    public NotifyUser(AzureNotificationService notifications) => Notifications = notifications;
    ```

2. Create your message using the `Message` class from `Sparc.Notifications.Azure`. This class represents a generic message
that will be automatically translated into Android, iOS, and Web push formats for Firebase and APS.

3. Send your message using the `SendAsync` function.
 
    ```csharp
    Message message = new("Time to wake up!", "Wake up, sleepyhead. It's time to face the new day!");

    // Send to a specific user
    await notifications.SendAsync(userId, message);

    // Send to a specific device
    await notifications.SendAsync(userId, deviceId, message);

    // Send to a specific group of users
    await notifications.SendAsync(message, "user-group-1");
    ```


