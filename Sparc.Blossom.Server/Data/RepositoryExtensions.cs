using System.Linq.Dynamic.Core;

namespace Sparc.Blossom.Data;

public static class RepositoryExtensions
{
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return queryable.ToDynamicListAsync<T>();
    }
}