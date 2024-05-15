using Sparc.Blossom.Data;

namespace Sparc.Blossom.Api;

public class BlossomApiContext<T>(IRunner<T> runner)
{
    public IRunner<T> Runner { get; } = runner;
}
