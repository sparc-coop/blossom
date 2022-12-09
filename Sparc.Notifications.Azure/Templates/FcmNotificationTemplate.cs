using Microsoft.Azure.NotificationHubs;
using System.Text.Json;

namespace Sparc.Blossom.Realtime;

public class FcmNotificationTemplate : InstallationTemplate
{
    public FcmNotificationTemplate() : base()
    {
        notification = new("$(title)", "$(body)", "$(image)");
        Body = JsonSerializer.Serialize(new
        {
            notification
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }); 
    }

    protected Notification notification { get; set; }
    
    public record Notification(string Title, string Body, string? Image);

}