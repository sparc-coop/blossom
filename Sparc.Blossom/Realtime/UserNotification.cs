namespace Sparc.Blossom;

public record UserNotification(string UserId) : MediatR.INotification;
