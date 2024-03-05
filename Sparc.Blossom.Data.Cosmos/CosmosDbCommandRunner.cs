using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbCommandRunner<T>(DbContext context) : ICommandRunner<T> where T : Entity<string>
{
    public DbContext Context { get; } = context;

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await Context.Set<T>().FindAsync(id)
            ?? throw new Exception($"Item with id {id} not found");

        await ExecuteAsync(entity, action);
    }

    public async Task DeleteAsync(object id)
    {
        var entity = await Context.Set<T>().FindAsync(id)
            ?? throw new Exception($"Item with id {id} not found");

        await DeleteAsync(entity);
    }

    private async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await SaveAsync(entity);
    }

    private async Task DeleteAsync(T item)
    {
        await DeleteAsync([item]);
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Context.Set<T>().Remove(item);

        await Context.SaveChangesAsync();
    }

    private async Task SaveAsync(T item)
    {
        await SaveAsync([item]);
    }

    private async Task SaveAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            var existing = await Context.Set<T>().FindAsync(item.Id);
            if (existing != null)
            {
                Context.Entry(existing).State = EntityState.Detached;
                Context.Add(item);
                Context.Update(item);
            }
            else
            {
                Context.Add(item);
            }
        }

        await Context.SaveChangesAsync();
    }

   
}
