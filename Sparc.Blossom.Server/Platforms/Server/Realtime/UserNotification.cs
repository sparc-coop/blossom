namespace Sparc.Blossom.Realtime;

public record UserNotification(string UserId) : MediatR.INotification;
