using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Blossom.Data;

public class BogusRepository<T> : IRepository<T> where T : BlossomEntity<string>
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

    public void BeginBulkOperation()
    {
        throw new NotImplementedException();
    }

    public Task CommitAsync()
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(IEnumerable<T> items)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(IEnumerable<T> items)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(IEnumerable<T> items)
    {
        throw new NotImplementedException();
    }

    public Task<T> FindAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}
