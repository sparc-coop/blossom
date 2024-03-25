using Ardalis.Specification;

namespace Sparc.Blossom.Data;

public interface IQueryRunner<T> where T : Entity<string>
{
    Task<T?> GetAsync(object id);
    Task<T?> GetAsync(ISpecification<T> spec);
    Task<U?> GetAsync<U>(ISpecification<T, U> spec);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(Func<T, bool> predicate);  
    Task<IEnumerable<T>> GetAllAsync(ISpecification<T> spec);
    Task<IEnumerable<U>> GetAllAsync<U>(ISpecification<T, U> spec);
    Task<IEnumerable<T>> GetAllAsync(string sql, params object[] parameters);
    Task<IEnumerable<U>> GetAllAsync<U>(string sql, params object[] parameters);
}