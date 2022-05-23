using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Core
{
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
        Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters);
        Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters);
    }
    
    public interface IRepository<T, TId> where T : IRoot<TId>
    {
        
    }
}