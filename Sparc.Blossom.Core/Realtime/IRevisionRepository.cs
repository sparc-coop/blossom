
namespace Sparc.Blossom.Data;

public interface IRevisionRepository<T> where T : BlossomEntity<string>
{
    Task<BlossomRevision<T>?> GetLatestAsync(string id);
    Task<BlossomRevision<T>?> GetAsync(string id, long revision);
    Task<BlossomRevision<T>?> GetAsync(string id, DateTime asOfDate);
    Task<IEnumerable<BlossomRevision<T>>> GetRevisionsAsync(string id, int count);
    Task<BlossomRevision<T>> RevertAsync(string id, long revision);
}