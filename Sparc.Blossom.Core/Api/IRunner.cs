namespace Sparc.Blossom.Data;

public interface IRunner<T>
{
    Task<T> CreateAsync(params object[] parameters);
    Task<T?> GetAsync(object id);
    Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters);  
    Task ExecuteAsync(object id, string name, params object[] parameters);
    Task DeleteAsync(object id);
    Task OnAsync(object id, string name, params object[] parameters);
}
