using Ardalis.Specification;
using Sparc.Core;

namespace Sparc.Kernel;

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
        if (typeof(T).IsAssignableTo(typeof(IRoot<string>)))
        {
            var itemsWithStringIds = _items.Cast<IRoot<string>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        if (typeof(T).IsAssignableTo(typeof(IRoot<int>)))
        {
            var itemsWithStringIds = _items.Cast<IRoot<int>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        throw new Exception("The items repository is not an IRoot.");
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
        object? id = (item as IRoot<string>)?.Id;
        if (id == null)
            id = (item as IRoot<int>)?.Id;

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
