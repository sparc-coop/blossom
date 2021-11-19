using Sparc.Core;
using System.Linq;

namespace Sparc.Database.SqlServer
{
    public static class SqlServerExtensions
    {
        public static IQueryable<T> Include<T>(this IRepository<T> repository, params string[] paths) where T : class
        {
            return ((SqlServerRepository<T>)repository).Include(paths);
        }
    }
}
