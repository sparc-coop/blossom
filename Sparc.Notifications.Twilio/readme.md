# Sparc.Notifications.Twilio

[![Nuget](https://img.shields.io/nuget/v/Sparc.Notifications.Twilio?label=Sparc.Notifications.Twilio)](https://www.nuget.org/packages/Sparc.Notifications.Twilio/)

The `Sparc.Notifications.Twilio` plugin is a plugin that enables low-configuration email and SMS notifications in your application, using [Twilio](https://www.twilio.com/) as the provider.

## Get Started with Sparc.Notifications.Twilio

In Your Features Project:

1. Add the `Sparc.Notifications.Twilio` Nuget package:
[![Nuget](https://img.shields.io/nuget/v/Sparc.Notifications.Twilio?label=Sparc.Notifications.Twilio)](https://www.nuget.org/packages/Sparc.Notifications.Twilio/)
2. Add the following settings to your `appsettings.json` file:
	```json
	{
        "Twilio": {
            "AccountSid": "", // the SID from your Twilio account
            "AuthToken": "", // the secret Auth Token from your Twilio account (preferably store this in usersecrets.json)
            "FromPhoneNumber": "+1555121212", // the configured Twilio Phone Number to send SMSs
            "FromEmailAddress": "info@myapp.com", // the email address all emails will come from
            "FromName": "My App", // the From name for all outbound emails
            "SendGridApiKey": "", // the SendGrid API key from your Twilio / Sendgrid account
    }
	```

3. Add the following line of code to your `Startup.cs` file to register the `Sparc.Notifications.Twilio` plugin. It will automatically read the configuration from the `Twilio` section of your `appsettings.json` file.
    ```csharp
    services.AddTwilio(Configuration);
	```

5. Inject `TwilioService` into any feature that needs to send emails or SMS messages. For example:
    ```csharp
    public class SendRegistrationNotification : Feature<bool>
    {
        IRepository<User> Users { get; set; }
        TwilioService Twilio { get; set; }
        
        public SendRegistrationNotification(IRepository<User> users, TwilioService twilio)
        {
            Users = users;
            Twilio = twilio;
        }

        public override async Task<bool> ExecuteAsync()
        {
            var user = await Users.FindAsync(User.Id());
            var message = "You have successfully registered!";
            
            if (user.PhoneNumber != null)
                await Twilio.SendSmsAsync(user.PhoneNumber, message);
            else 
                await Twilio.SendEmailAsync(user.Email, message);

           // or simply call Twilio.SendAsync and pass in either an email or phone number, and the plugin will send an SMS or email accordingly
           await Twilio.SendAsync(user.PhoneNumber ?? user.Email, message);

           return true;
        }
    }
    ```
    
   You can check a real example at the [InviteUser Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Users/InviteUser.cs)
