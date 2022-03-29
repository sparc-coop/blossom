using System.Text.Json;

namespace Sparc.Notifications.Azure
{
    public class AndroidNotificationTemplate : FcmNotificationTemplate
    {
        public AndroidNotificationTemplate() : base()
        {
            Android android = new("$(priority)", new("$(title)",
                "$(body)",
                "$(image)",
                "$(icon)",
                "$(color)",
                "$(sound)",
                "$(clickaction)",
                "$(channel)",
                "$(clientpriority)",
                "$(visibility)"));

            Body = JsonSerializer.Serialize(new
            {
                notification,
                android
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        }

        public record Android(string Priority, AndroidNotification Notification);
        public record AndroidNotification(string? Title, string? Body, string? Image, string? Icon, string? Color, string? Sound, string? Click_Action, string? Channel_Id, string? Notification_Priority, string? Visibility);
    }
}