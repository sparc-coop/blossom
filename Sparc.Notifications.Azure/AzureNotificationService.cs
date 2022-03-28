using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Notifications.Azure
{
    public class AzureNotificationService
    {
        public AzureNotificationService(AzureConfiguration configuration)
        {
            Hub = NotificationHubClient.CreateClientFromConnectionString(configuration.ConnectionString, configuration.HubName);
        }

        public NotificationHubClient Hub { get; }

        public async Task<bool> RegisterAsync(string userId, string deviceId, Platforms platform, params string[] tags)
        {
            var installation = new Installation
            {
                InstallationId = $"{userId}|{deviceId}",
                UserId = userId,
                PushChannel = deviceId,
                Tags = tags,
                Platform = platform switch
                {
                    Platforms.Windows => NotificationPlatform.Wns,
                    Platforms.iOS => NotificationPlatform.Apns,
                    Platforms.Android => NotificationPlatform.Fcm,
                    _ => throw new Exception("Invalid platform")
                }
            };
            
            try
            {
                await Hub.CreateOrUpdateInstallationAsync(installation);
                return true;
            }
            catch (MessagingException)
            {
                return false;
            }
        }

        public async Task<bool> SendAsync(string userId, string deviceId, string message)
        {
            var installationId = $"{userId}|{deviceId}";
            string[] userTag = new string[2];
            userTag[0] = "$InstallationId:{" + installationId + "}";

            var installation = await Hub.GetInstallationAsync(installationId);

            NotificationOutcome outcome = installation.Platform switch
            {
                NotificationPlatform.Wns => await Hub.SendWindowsNativeNotificationAsync(WindowsTemplate(message), userTag),
                NotificationPlatform.Apns => await Hub.SendAppleNativeNotificationAsync(AppleTemplate(message), userTag),
                NotificationPlatform.Fcm => await Hub.SendFcmNativeNotificationAsync(AndroidTemplate(message), userTag),
                _ => throw new Exception("Invalid platform")
            };

            return outcome.State != NotificationOutcomeState.Abandoned && outcome.State != NotificationOutcomeState.Unknown;
        }

        public async Task<bool> SendAsync(Platforms platform, string userId, string message)
        {
            string[] userTag = new string[2];
            userTag[0] = "username:" + userId;
            //userTag[1] = "from:" + user;

            NotificationOutcome outcome = platform switch
            {
                Platforms.Windows => await Hub.SendWindowsNativeNotificationAsync(WindowsTemplate(message), userTag),
                Platforms.iOS => await Hub.SendAppleNativeNotificationAsync(AppleTemplate(message), userTag),
                Platforms.Android => await Hub.SendFcmNativeNotificationAsync(AndroidTemplate(message), userTag),
                _ => throw new Exception("Invalid platform")
            };

            return outcome.State != NotificationOutcomeState.Abandoned && outcome.State != NotificationOutcomeState.Unknown;
        }


        private static string WindowsTemplate(string message) => @"<toast><visual><binding template=""ToastText01""><text id=""1"">"
        + message
        + "</text></binding></visual></toast>";

        private static string AppleTemplate(string message) => "{\"aps\":{\"alert\":\"" + message + "\"}}";

        private static string AndroidTemplate(string message) => "{ \"data\" : {\"message\":\"" + message + "\"}}";
    }
}