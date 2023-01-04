using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public interface IRoot<T>
{
    T Id { get; set; }
    void Broadcast(INotification notification);
}