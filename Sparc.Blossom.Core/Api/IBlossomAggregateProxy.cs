namespace Sparc.Blossom;

public interface IBlossomAggregateProxy<T>
{
    IRunner<T> Runner { get; }
}
