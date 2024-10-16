
namespace Sparc.Blossom.Data;

public class BlossomInMemoryRevisionTracker<T> : IRevisionRepository<T> where T : BlossomEntity<string>
{
    public BlossomInMemoryDb<BlossomRevision<T>> Db { get; set; } = new();
    
    public Task<BlossomRevision<T>?> FindAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<BlossomRevision<T>?> FindAsync(string id, long revision)
    {
        throw new NotImplementedException();
    }

    public Task<BlossomRevision<T>?> FindAsync(string id, DateTime asOfDate)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BlossomRevision<T>>> GetAsync(string id, int count)
    {
        throw new NotImplementedException();
    }

    public Task<BlossomRevision<T>> ReplaceAsync(string id, long revision)
    {
        throw new NotImplementedException();
    }
}
