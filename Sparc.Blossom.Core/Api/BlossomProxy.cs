using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Api;

public interface IBlossomEntityProxy
{
    string SubscriptionId { get; }
    Task Create<T>(T item);
    Task Update(IEnumerable<BlossomPatch> patches);
    Task Update();
    Task Delete();
    Task Delete<T>(T item);
}

public interface IBlossomProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomProxy<T> : IBlossomProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
}

public class BlossomEntityProxy<T, TId> : BlossomProxy<T>, IBlossomEntityProxy
{
    public TId Id { get; set; } = default!;
    public string SubscriptionId => $"{GetType().Name}-{Id}";

    public async Task Create<TItem>(TItem item) => await Runner.Add(Id, item);

    public Task Update(IEnumerable<BlossomPatch> patches)
    {
        foreach (var patch in patches)
            patch.ApplyTo(this);

        return Task.CompletedTask;
    }

    public async Task Update() => await Runner.Patch(Id, this);
    public async Task Delete() => await Runner.Delete(Id);
    public async Task Delete<TItem>(TItem item) => await Runner.Remove(Id, item);
}