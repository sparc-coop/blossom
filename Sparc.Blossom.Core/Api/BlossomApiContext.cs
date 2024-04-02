using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public class BlossomApiContext<T>(IRunner<T> runner) where T : Entity<string>
{
    public IRunner<T> Runner { get; } = runner;
}
