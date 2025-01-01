namespace Sparc.Blossom;

public class BlossomProxy<T>(IRepository<T> repository) where T : BlossomEntity
{
    protected IRepository<T> Repository { get; } = repository;
    protected BlossomEntityProxy<T> ToProxy(T entity) => new(entity, Repository);
}
