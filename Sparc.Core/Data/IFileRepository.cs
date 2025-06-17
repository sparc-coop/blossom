namespace Sparc.Blossom;

public interface IFileRepository<T>
{
    // Commands
    Task AddAsync(T item);
    Task AddAsync(IEnumerable<T> items);
    Task UpdateAsync(T item);
    Task UpdateAsync(IEnumerable<T> items);
    Task DeleteAsync(T item);
    Task DeleteAsync(IEnumerable<T> items);
    Task<T?> FindAsync(object id);
}