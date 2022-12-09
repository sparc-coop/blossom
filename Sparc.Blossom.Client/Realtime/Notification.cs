namespace Sparc.Blossom;

public class Notification
{
    public Notification(string? subscriptionId = null)
    {
        SubscriptionId = subscriptionId;
    }

    public string? SubscriptionId { get; set; }
}