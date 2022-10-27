using Sparc.Realtime;

namespace Sparc.Core;

public interface ISparcRoot
{
    public List<SparcNotification>? Events { get; }
}

public class SparcRoot<T> : Root<T>, ISparcRoot where T : notnull
{
    private List<SparcNotification>? _events;
    public List<SparcNotification>? Events => _events;

    public void Broadcast(SparcNotification notification)
    {
        _events ??= new List<SparcNotification>();
        _events.Add(notification);
    }
}