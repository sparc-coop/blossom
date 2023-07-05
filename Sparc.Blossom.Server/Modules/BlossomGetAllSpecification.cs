using Ardalis.Specification;
using Sparc.Blossom.Data;
using System.Linq.Expressions;

namespace Sparc.Blossom;

public class BlossomGetAllSpecification<T> : Specification<T> where T : Entity<string>
{
    public BlossomGetAllSpecification(Expression<Func<T, bool>>? filterQuery = null, int? take = null)
    {
        if (filterQuery != null)
            Query.Where(filterQuery);
        
        if (take != null)
            Query.Take(take.Value);
    }
}