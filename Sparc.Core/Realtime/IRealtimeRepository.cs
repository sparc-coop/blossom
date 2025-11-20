namespace Sparc.Blossom;

public interface IRealtimeRepository<T> where T : BlossomEntity
{
    Task<BlossomEntityChanged<T>?> GetAsync(string id, long revision);
    Task<BlossomEntityChanged<T>?> GetAsync(string id, DateTime? asOfDate = null);
    Task<IEnumerable<BlossomEntityChanged<T>>> GetAllAsync(string id, int? count = null);
    Task<T> ReplaceAsync(string id, long revision);
    Task<T> ReplaceAsync(BlossomEntityChanged<T> current, long revision);
    Task BroadcastAsync(string eventName, T entity);
    Task BroadcastAsync(BlossomEntityChanged<T> blossomEvent);
    Task<T?> UndoAsync(string id);
    Task<T?> RedoAsync(string id);
}