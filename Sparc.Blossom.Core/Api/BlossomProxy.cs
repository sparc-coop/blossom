namespace Sparc.Blossom;

public class BlossomProxy<T>(IRepository<T> repository) where T : BlossomEntity
{
    protected IRepository<T> Repository { get; } = repository;
    protected BlossomEntityProxy<T> ToProxy(T entity) => new(entity, Repository);
    protected TProxy ToProxy<TProxy>(T entity) where TProxy : BlossomEntityProxy<T> => new(entity, Repository) as TProxy;
}
