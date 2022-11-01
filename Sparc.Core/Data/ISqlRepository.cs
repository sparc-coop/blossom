namespace Sparc.Core;

public interface ISqlRepository<T> : IRepository<T>
{
    Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters);
    Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters);
}
