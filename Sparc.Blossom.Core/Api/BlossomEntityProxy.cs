namespace Sparc.Blossom.Api;

public interface IBlossomProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomEntityProxy<T, TId> : IBlossomProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
    public TId Id { get; set; } = default!;
}

public record BlossomRecordProxy<T> : IBlossomProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
}

