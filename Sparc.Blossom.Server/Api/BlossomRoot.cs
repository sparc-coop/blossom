using Sparc.Blossom.Api;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public class BlossomRoot<T>(IRepository<T> repository) where T : Entity
{
    public IRepository<T> Repository { get; } = repository;
    protected BlossomApiContext<T> Api { get; } = new();
}

