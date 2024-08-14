using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public static class RepositoryExtensions
{
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return EntityFrameworkQueryableExtensions.ToListAsync(queryable);
    }

    public static void ToUrl<T>(this IRepository<T> set, string baseUrl) where T : class
    {
        if (set is BlossomSet<T> blossomSet)
            blossomSet.ToUrl(baseUrl);
    }
}