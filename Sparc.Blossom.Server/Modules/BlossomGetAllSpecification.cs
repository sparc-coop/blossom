using Ardalis.Specification;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public class BlossomGetAllSpecification<T> : Specification<T> where T : Entity<string>
{
    public BlossomGetAllSpecification(int? take = null)
    {
        if (take != null)
            Query.Take(take.Value);
    }
}