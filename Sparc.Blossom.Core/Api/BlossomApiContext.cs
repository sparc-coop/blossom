namespace Sparc.Blossom.Api;

public class BlossomApiContext<T>(IRunner<T> runner)
{
    public IRunner<T> Runner { get; } = runner;

    public async Task Delete(object id) => await Runner.DeleteAsync(id);
    public async Task<T?> Get(object id) => await Runner.GetAsync(id);

}
