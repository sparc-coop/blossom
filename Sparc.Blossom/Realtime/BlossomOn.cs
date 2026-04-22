namespace Sparc.Blossom.Realtime;

public interface IBlossomOn
{
    Task ExecuteAsync<T>(T item);
}

public abstract class BlossomOn<T> : IBlossomOn
{
    public abstract Task ExecuteAsync(T item);

    async Task IBlossomOn.ExecuteAsync<TItem>(TItem item)
    {
        if (item is T typedItem)
            await ExecuteAsync(typedItem);
    }
}