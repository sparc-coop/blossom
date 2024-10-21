
namespace Sparc.Blossom.Data;

public interface IRevisionRepository<T> where T : BlossomEntity
{
    Task<BlossomRevision<T>?> GetAsync(string id, long revision);
    Task<BlossomRevision<T>?> GetAsync(string id, DateTime? asOfDate = null);
    Task<IEnumerable<BlossomRevision<T>>> GetAllAsync(string id, int count);
    Task<BlossomRevision<T>> UndoAsync(string id);
    Task<BlossomRevision<T>> RedoAsync(string id);

    Task AddAsync(string id);
    Task<BlossomRevision<T>> ReplaceAsync(string id, long revision);
}