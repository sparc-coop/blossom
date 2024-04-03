namespace Sparc.Blossom.Data;

public interface IRunner<T>
{
    Task<T?> GetAsync(object id);
    Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters);  
    Task ExecuteAsync(object id, string name, params object[] parameters);
    Task OnAsync(object id, string name, params object[] parameters);
}
