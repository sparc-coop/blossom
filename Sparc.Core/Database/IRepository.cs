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
        Task UpdateAsync(T item);
        Task DeleteAsync(T item);
        Task ExecuteAsync(object id, Action<T> action);
        Task ExecuteAsync(T entity, Action<T> action);
        Task<T?> FindAsync(object id);
        Task CommitAsync();
        Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters);
        Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters);
        void BeginBulkOperation();

    }
    
    public interface IRepository<T, TId> where T : IRoot<TId>
    {
        
    }
}