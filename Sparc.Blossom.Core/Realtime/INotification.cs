namespace Sparc.Blossom.Realtime;

public interface INotification : MediatR.INotification
{
    public string SubscriptionId { get; set; }
}