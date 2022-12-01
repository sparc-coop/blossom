using Sparc.Core;

namespace Sparc.Database.SqlServer;

public static class SqlServerExtensions
{
    public static IQueryable<T> Include<T>(this IRepository<T> repository, params string[] paths) where T : class
    {
        return ((SqlServerRepository<T>)repository).Include(paths);
    }

    public static IQueryable<T> TemporalAll<T>(this IRepository<T> repository) where T : class
    {
        return ((SqlServerRepository<T>)repository).TemporalAll();
    }

    public static IQueryable<T> TemporalAsOf<T>(this IRepository<T> repository, DateTime asOfDate) where T : class
    {
        return ((SqlServerRepository<T>)repository).TemporalAsOf(asOfDate);
    }

    public static IQueryable<T> TemporalBetween<T>(this IRepository<T> repository, DateTime fromDate, DateTime toDate) where T : class
    {
        return ((SqlServerRepository<T>)repository).TemporalBetween(fromDate, toDate);
    }
}
