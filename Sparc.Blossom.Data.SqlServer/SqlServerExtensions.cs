using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Sparc.Blossom.Data;

public static class SqlServerExtensions
{
    public static IQueryable<T> Include<T>(this IRepository<T> repository, params string[] paths) where T : class
    {
        return ((SqlServerRepository<T>)repository).Include(paths);
    }
}
