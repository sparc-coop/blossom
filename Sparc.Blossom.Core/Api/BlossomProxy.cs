namespace Sparc.Blossom.Api;

public interface IBlossomProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomProxy<T> : IBlossomProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
}

public class BlossomEntityProxy<T, TId> : BlossomProxy<T>
{
    public TId Id { get; set; } = default!;
}