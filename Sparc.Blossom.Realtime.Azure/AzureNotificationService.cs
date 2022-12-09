using Microsoft.Azure.NotificationHubs;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

public class AzureNotificationService
{
    public AzureNotificationService(AzureConfiguration configuration)
    {
        Hub = NotificationHubClient.CreateClientFromConnectionString(configuration.ConnectionString, configuration.HubName);
    }

    public NotificationHubClient Hub { get; }

    public async Task<bool> RegisterAsync(string userId, IDevice device)
    {
        var installation = new Installation
        {
            InstallationId = $"{userId}-{device.Id}",
            UserId = userId,
            PushChannel = device.PushToken,
            Tags = new string[] { "default" },
            Templates = new Dictionary<string, InstallationTemplate>(),
            Platform = device.Platform switch
            {
                "Windows" => NotificationPlatform.Wns,
                "iOS" => NotificationPlatform.Apns,
                "Android" => NotificationPlatform.Fcm,
                "Web" => NotificationPlatform.Fcm,
                _ => throw new Exception("Invalid platform")
            }
        };

        InstallationTemplate defaultTemplate = device.Platform switch
        {
            "Windows" => new WindowsNotificationTemplate(),
            "Android" => new AndroidNotificationTemplate(),
            "iOS" => new IosNotificationTemplate(),
            "Web" => new WebNotificationTemplate(),
            _ => throw new Exception("Invalid platform")
        };

        installation.Templates.Add("default", defaultTemplate);

        await Hub.CreateOrUpdateInstallationAsync(installation);
        return true;
    }

    public async Task<bool> SendAsync(Message message, params string[] tags)
    {
        NotificationOutcome outcome = await Hub.SendTemplateNotificationAsync(message.ToDictionary(), tags);
        return outcome.State != NotificationOutcomeState.Abandoned && outcome.State != NotificationOutcomeState.Unknown;
    }

    public async Task<bool> SendAsync(string userId, Message message) => await SendAsync(message, "$UserId:{" + userId + "}");

    public async Task<bool> SendAsync(string userId, string deviceId, Message message)
        => await SendAsync(message, "$InstallationId:{" + userId + "-" + deviceId + "}");

    public async Task<bool> ScheduleAsync(Message message, DateTime scheduledTime, params string[] tags)
    {
        var notification = new TemplateNotification(message.ToDictionary());
        await Hub.ScheduleNotificationAsync(notification, scheduledTime, tags);
        return true;
    }

    public async Task<bool> ScheduleAsync(string userId, Message message, DateTime scheduledTime) => await ScheduleAsync(message, scheduledTime, "$UserId:{" + userId + "}");

    public async Task<bool> ScheduleAsync(string userId, string deviceId, Message message, DateTime scheduledTime)
        => await ScheduleAsync(message, scheduledTime, "$InstallationId:{" + userId + "-" + deviceId + "}");
}