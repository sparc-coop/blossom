using Microsoft.Azure.NotificationHubs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public class IosNotificationTemplate : InstallationTemplate
{
    public IosNotificationTemplate() : base()
    {
        Alert alert = new(
            "$(title)", 
            "$(subtitle)",
            "$(body)",
            "$(image)");

        Body = JsonSerializer.Serialize(new
        {
            aps = new
            {
                alert,
                category = "$(channel)",
                interruption_level = "$(interruptionlevel)"
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    }

    public record Alert
    {
        public string Title { get; set; }

        public Alert(string title, string? subtitle, string body, string launchImage)
        {
            Title = title;
            Subtitle = subtitle;
            Body = body;
            LaunchImage = launchImage;
        }

        public string? Subtitle { get; set; }
        public string Body { get; set; }
        [JsonPropertyName("launch-image")]
        public string LaunchImage { get; set; }
    }
}