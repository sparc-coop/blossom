using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Sparc.Features
{
    public static class RepositoryExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
        {
            return queryable.ToDynamicListAsync<T>();
        }
    }
}