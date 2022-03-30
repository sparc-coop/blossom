using Sparc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Tests
{
    public class BogusRepository<T> : IRepository<T> where T : class, IRoot<string>
    {
        private readonly List<T> _data;

        public BogusRepository()
        {
            _data = new List<T>();
        }
        
        public BogusRepository(IEnumerable<T> data)
        {
            _data = data.ToList();
        }

        public IQueryable<T> Query => _data.AsQueryable();

        public Task AddAsync(T item)
        {
            _data.Add(item);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<T> items)
        {
            _data.AddRange(items);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T item)
        {
            _data.Remove(item);
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(object id, Action<T> action)
        {
            var datum = await FindAsync(id);
            await ExecuteAsync(datum, action);
        }

        public Task ExecuteAsync(T entity, Action<T> action)
        {
            action(entity);
            return Task.CompletedTask;
        }

        public Task<T> FindAsync(object id)
        {
            var idAsString = id.ToString();
            return Task.FromResult(_data.FirstOrDefault(x => x.Id == idAsString));
        }
        
        public async Task UpdateAsync(T item)
        {
            var idAsString = item.Id.ToString();
            var index = _data.FindIndex(x => x.Id == idAsString);
            if (index > -1)
                _data[index] = item;
            else
                await AddAsync(item);
        }

        public Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters)
        {
            throw new NotImplementedException();
        }

        public void BeginBulkOperation()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }
    }
}
