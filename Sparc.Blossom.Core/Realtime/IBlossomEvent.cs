namespace Sparc.Blossom.Realtime;

public interface IBlossomEvent : MediatR.INotification
{
    public string? SubscriptionId { get; }
}