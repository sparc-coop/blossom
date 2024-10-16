namespace Sparc.Blossom.Data;

public interface IRevisionRepository<T> where T : BlossomEntity<string>
{
    Task<BlossomRevision<T>?> FindAsync(string id);
    Task<BlossomRevision<T>?> FindAsync(string id, long revision);
    Task<IEnumerable<BlossomRevision<T>>> GetAsync(string id, int count);
    Task<BlossomRevision<T>> ReplaceAsync(string id, long revision);
}