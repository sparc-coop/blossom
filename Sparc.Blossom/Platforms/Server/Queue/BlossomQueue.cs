using System.Threading.Channels;

namespace Sparc.Blossom;

public interface IBlossomQueue<T>
{
    ValueTask PushAsync(Func<T, CancellationToken, ValueTask> workItem);

    ValueTask<Func<T, CancellationToken, ValueTask>> PopAsync(
        CancellationToken cancellationToken);
}

public class BlossomQueue<T>(int capacity) : IBlossomQueue<T>
{
    // Capacity should be set based on the expected application load and
    // number of concurrent threads accessing the queue.
    // BoundedChannelFullMode.Wait causes calls to WriteAsync() to return a task,
    // which only completes when space becomes available. In the event that many
    // publishers/calls start accumulating, using BoundedChannelFullMode.Wait can
    // lead to backpressure.
    private readonly Channel<Func<T, CancellationToken, ValueTask>> _queue =
        Channel.CreateBounded<Func<T, CancellationToken, ValueTask>>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public async ValueTask PushAsync(Func<T, CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<T, CancellationToken, ValueTask>> PopAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}