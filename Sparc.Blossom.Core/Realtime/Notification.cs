namespace Sparc.Blossom;

public class Notification : MediatR.INotification
{
    public Notification(string? subscriptionId = null)
    {
        SubscriptionId = subscriptionId;
    }

    public string? SubscriptionId { get; set; }
}