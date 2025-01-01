using Ardalis.Specification;

namespace Sparc.Blossom;

public class BlossomCollectionProxy<T>(IRepository<T> repository) : BlossomProxy<T>(repository)
    where T : BlossomEntity
{
    public async Task<BlossomQueryResult<T>> Query(BlossomQueryOptions options) => throw new NotImplementedException(); //await Repository.FindAsync(new BlossomQuery<T>(Repository)..ExecuteQuery(options);

    public async Task<BlossomEntityProxy<T>?> FindAsync(object id)
    {
        var entity = await Repository.FindAsync(id);
        return entity == null ? null : ToProxy(entity);
    }

    public async Task<IEnumerable<BlossomEntityProxy<T>>> GetAllAsync(ISpecification<T> spec)
    {
        var results = await Repository.GetAllAsync(spec);
        var proxies = results.Select(ToProxy);
        return proxies;
    }
}
