
namespace Sparc.Blossom.Data
{
    public interface ICommandRunner<T> where T : Entity<string>
    {
        Task ExecuteAsync(object id, Action<T> action);
        Task DeleteAsync(object id);
    }
}