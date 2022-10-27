namespace Sparc.Realtime;

public record SparcNotification(string? GroupId = null, string? UserId = null) : MediatR.INotification;