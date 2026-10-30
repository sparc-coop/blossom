using System.Threading.Channels;

namespace Sparc.Blossom;

public interface IBlossomJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IBlossomQueue<T>
{
    ValueTask PushAsync(T message);
    ValueTask<T> PopAsync(CancellationToken cancellationToken);
}

public class BlossomQueue<T>(int capacity) : IBlossomQueue<T>
{
    // Capacity should be set based on the expected application load and
    // number of concurrent threads accessing the queue.
    // BoundedChannelFullMode.Wait causes calls to WriteAsync() to return a task,
    // which only completes when space becomes available. In the event that many
    // publishers/calls start accumulating, using BoundedChannelFullMode.Wait can
    // lead to backpressure.
    private readonly Channel<T> _queue =
        Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public async ValueTask PushAsync(T message)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _queue.Writer.WriteAsync(message);
    }

    public async ValueTask<T> PopAsync(CancellationToken cancellationToken)
    {
        var message = await _queue.Reader.ReadAsync(cancellationToken);
        return message;
    }
}