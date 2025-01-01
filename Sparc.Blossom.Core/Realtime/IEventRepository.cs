namespace Sparc.Blossom;

public interface IEventRepository<T> where T : BlossomEntity
{
    Task<BlossomEvent<T>?> GetAsync(string id, long revision);
    Task<BlossomEvent<T>?> GetAsync(string id, DateTime? asOfDate = null);
    Task<IEnumerable<BlossomEvent<T>>> GetAllAsync(string id, int? count = null);
    Task<T> ReplaceAsync(string id, long revision);
    Task<T> ReplaceAsync(BlossomEvent<T> current, long revision);
    Task AddAsync(string eventName, T entity);
    Task AddAsync(BlossomEvent<T> blossomEvent);
    Task<T?> UndoAsync(string id);
    Task<T?> RedoAsync(string id);
}