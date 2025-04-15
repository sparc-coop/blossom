using Ardalis.Specification;

namespace Sparc.Blossom;

public interface IRepository<T>
{
    // Queries
    IQueryable<T> Query { get; }

    // Commands
    Task AddAsync(T item);
    Task AddAsync(IEnumerable<T> items);
    Task UpdateAsync(T item);
    Task UpdateAsync(IEnumerable<T> items);
    Task DeleteAsync(T item);
    Task DeleteAsync(IEnumerable<T> items);
    Task ExecuteAsync(object id, Action<T> action);
    Task ExecuteAsync(T entity, Action<T> action);
    Task<T?> FindAsync(object id);
    Task<T?> FindAsync(ISpecification<T> spec);
    Task<List<T>> GetAllAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
    Task<int> CountAsync();
    Task<bool> AnyAsync(ISpecification<T> spec);
    IQueryable<T> FromSqlRaw(string sql, params object[] parameters);
    Task<List<T>> SyncAsync();
}