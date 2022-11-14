using Ardalis.Specification;
using Sparc.Core;
using Sparc.Kernel;

namespace Sparc.Data;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private static readonly List<T> _items = new();

    public IQueryable<T> Query => _items.AsQueryable();

    public Task AddAsync(T item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await AddAsync(item);
    }

    public Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).Any());
    }

    public Task<int> CountAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).Count());
    }

    public Task DeleteAsync(T item)
    {
        _items.Remove(item);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await DeleteAsync(item);
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var item = await FindAsync(id);
        if (item != null)
            await ExecuteAsync(item, action);
    }

    public Task ExecuteAsync(T entity, Action<T> action)
    {
        if (entity != null)
            action(entity);

        return Task.CompletedTask;
    }

    public Task<T?> FindAsync(object id)
    {
        if (typeof(T).IsAssignableTo(typeof(Root<string>)))
        {
            var itemsWithStringIds = _items.Cast<Root<string>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        if (typeof(T).IsAssignableTo(typeof(Root<int>)))
        {
            var itemsWithStringIds = _items.Cast<Root<int>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        throw new Exception("The item for this repository is not a Root.");
    }

    public Task<T?> FindAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).FirstOrDefault());
    }

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).ToList());
    }

    public async Task UpdateAsync(T item)
    {
        object? id = (item as Root<string>)?.Id;
        id ??= (item as Root<int>)?.Id;

        if (id == null)
            throw new Exception("The item passed to UpdateAsync has no Id set.");

        var existingItem = await FindAsync(id);
        if (existingItem != null)
            await DeleteAsync(existingItem);

        await AddAsync(item);
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await UpdateAsync(item);
    }
}
