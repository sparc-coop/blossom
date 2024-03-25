using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbQueryRunner<T> : RepositoryBase<T>, IQueryRunner<T> where T : Entity<string>
{
    public IQueryable<T> Query { get; }
    public DbContext Context { get; }
    protected CosmosDbDatabaseProvider DbProvider { get; }

    private static bool IsCreated;
    public PartitionKey? PartitionKey { get; private set; }

    public CosmosDbQueryRunner(DbContext context, CosmosDbDatabaseProvider dbProvider) : base(context)
    {
        Context = context;
        DbProvider = dbProvider;
        if (!IsCreated)
        {
            Context.Database.EnsureCreatedAsync().Wait();
            IsCreated = true;
        }

        Query = context.Set<T>().AsNoTracking();
    }

    public async Task<T?> GetAsync(object id)
    {
        if (id is string sid)
            return Query.FirstOrDefault(x => x.Id == sid);

        return await Context.Set<T>().FindAsync(id);
    }

    public async Task<T?> GetAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Query.ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync(Func<T, bool> predicate)
    {
        return await Query.Where(predicate).AsQueryable().ToListAsync();
    }

    public async Task<U?> GetAsync<U>(ISpecification<T, U> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public IQueryable<T> GetAllAsync(string sql, params object[] parameters)
    {
        return CosmosQueryableExtensions.FromSqlRaw(Context.Set<T>(), sql, parameters);
    }

    public async Task<IEnumerable<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task<IEnumerable<U>> GetAllAsync<U>(string sql, params object[] parameters)
    {
        var container = DbProvider.Database.GetContainer(Context.GetType().Name);
        var requestOptions = PartitionKey == null
            ? null
            : new QueryRequestOptions { PartitionKey = PartitionKey };

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

    public Task<IEnumerable<U>> GetAllAsync<U>(ISpecification<T, U> spec)
    {
        throw new NotImplementedException();
    }

    Task<IEnumerable<T>> IQueryRunner<T>.GetAllAsync(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }
}
