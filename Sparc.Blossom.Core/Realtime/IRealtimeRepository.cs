
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public interface IRealtimeRepository<T> where T : BlossomEntity
{
    Task<BlossomEvent<T>?> GetAsync(string id, long revision);
    Task<BlossomEvent<T>?> GetAsync(string id, DateTime? asOfDate = null);
    Task<IEnumerable<BlossomEvent<T>>> GetAllAsync(string id, int? count = null);
    Task<T> ReplaceAsync(string id, long revision);
    Task<T> ReplaceAsync(BlossomEvent<T> current, long revision);
    Task BroadcastAsync(string eventName, T entity);
    Task BroadcastAsync(BlossomEvent<T> blossomEvent);
    Task<T?> UndoAsync(string id);
    Task<T?> RedoAsync(string id);
}