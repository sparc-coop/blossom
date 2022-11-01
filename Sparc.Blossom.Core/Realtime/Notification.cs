namespace Sparc.Blossom;

public record Notification(string? SubscriptionId = null) : MediatR.INotification;