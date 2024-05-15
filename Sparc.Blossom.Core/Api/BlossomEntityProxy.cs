using Sparc.Blossom.Data;

namespace Sparc.Blossom.Api;

public interface IBlossomEntityProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomEntityProxy<T, TId> : IBlossomEntityProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
    public TId Id { get; set; } = default!;
}

