using System.Linq.Expressions;

namespace Sparc.Blossom.Data;

public static class SqlServerExtensions
{
    public static IQueryable<T> Include<T>(this IRepository<T> repository, params string[] paths) where T : class
    {
        return ((SqlServerRepository<T>)repository).Include(paths);
    }

    public static IQueryable<T> Include<T, TProperty>(this IRepository<T> repository, params Expression<Func<T, TProperty>>[] paths) where T : class
    {
        return ((SqlServerRepository<T>)repository).Include(paths);
    }
}
