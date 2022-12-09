namespace Sparc.Blossom.Realtime;

public record Notification(string? SubscriptionId = null) : MediatR.INotification;
