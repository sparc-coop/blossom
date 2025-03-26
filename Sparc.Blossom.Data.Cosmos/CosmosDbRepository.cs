using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : BlossomEntity<string>
{
    public IQueryable<T> Query { get; }
    public DbContext Context { get; }
    protected CosmosDbDatabaseProvider DbProvider { get; }

    private static bool IsCreated;

    public CosmosDbRepository(DbContext context, CosmosDbDatabaseProvider dbProvider) : base(context)
    {
        Context = context;
        DbProvider = dbProvider;

        //if (!IsCreated)
        //{
        //    Context.Database.EnsureCreatedAsync().Wait();
        //    IsCreated = true;
        //}
        //Mediator = mediator;
        Query = context.Set<T>();
    }

    public async Task<T?> FindAsync(object id)
    {
        if (id is string sid)
            return await Context.Set<T>().FirstOrDefaultAsync(x => x.Id == sid);

        return await Context.Set<T>().FindAsync(id);
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await CountAsync(spec, default);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return await AnyAsync(spec, default);
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task AddAsync(T item)
    {
        await AddAsync([item]);
    }

    public virtual async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Context.Add(item);

        await SaveChangesAsync();
    }

    public async Task UpdateAsync(T item)
    {
        await UpdateAsync([item]);
    }

    public virtual async Task UpdateAsync(IEnumerable<T> items)
    {
        var detachedItems = items.Where(x => !IsTracked(x));
        
        foreach (var item in detachedItems)
        {
            var existing = await FindAsync(item.Id);
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

    private bool IsTracked(T entity) => Context.ChangeTracker.Entries<T>().Any(x => x.Entity.Id == entity.Id);

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await FindAsync(id);
        if (entity == null)
            throw new Exception($"Item with id {id} not found");

        await ExecuteAsync(entity, action);
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await UpdateAsync(entity);
    }

    public async Task DeleteAsync(T item)
    {
        await DeleteAsync([item]);
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Context.Set<T>().Remove(item);

        await Context.SaveChangesAsync();
    }

    private async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        return CosmosQueryableExtensions.FromSqlRaw(Context.Set<T>(), sql, parameters);
    }

    public IQueryable<T> FromSql(FormattableString sql)
    {
        return CosmosQueryableExtensions.FromSql(Context.Set<T>(), sql);
    }

    public async Task<List<U>> FromSqlAsync<U>(string sql, string? partitionKey, string? containerName = null, params object[] parameters)
    {
        var container = DbProvider.Database.GetContainer(containerName ?? Context.GetType().Name);
        var requestOptions = partitionKey == null
            ? null
            : new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };

        sql = sql.Replace("{", "@").Replace("}", "");

        var query = new QueryDefinition(sql);
        if (parameters != null)
        {
            var i = 0;
            foreach (var parameter in parameters)
            {
                var key = $"@{i++}";
                query = query.WithParameter(key, parameter);
            }
        }

        var results = container.GetItemQueryIterator<U>(query,
            requestOptions: requestOptions);

        var list = new List<U>();

        while (results.HasMoreResults)
            list.AddRange(await results.ReadNextAsync());

        return list;
    }

    public IQueryable<T> PartitionQuery(string partitionKey)
    {
        return Query.WithPartitionKey(partitionKey);
    }
}
