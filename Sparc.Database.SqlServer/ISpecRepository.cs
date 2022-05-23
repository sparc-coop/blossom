using Ardalis.Specification;
using Sparc.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sparc.Database.SqlServer;

public interface ISpecRepository<T> : IRepository<T>
{
    Task<T?> FindAsync(ISpecification<T> spec);
    Task<List<T>> GetAllAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
    Task<bool> AnyAsync(ISpecification<T> spec);
}
