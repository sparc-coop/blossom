using System.Threading.Channels;

namespace Sparc.Blossom;

public interface IBackgroundTaskQueue<T> where T : class
{
    ValueTask AddAsync(Func<T, CancellationToken, ValueTask> workItem);
    ValueTask<Func<T, CancellationToken, ValueTask>> GetAsync(CancellationToken cancellationToken);
}

public class BlossomQueue<T> : IBackgroundTaskQueue<T> where T : class
{
    private readonly Channel<Func<T, CancellationToken, ValueTask>> _queue;

    public BlossomQueue(int capacity = 200)
    {
        // Capacity should be set based on the expected application load and
        // number of concurrent threads accessing the queue.            
        // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
        // which completes only when space became available. This leads to backpressure,
        // in case too many publishers/calls start accumulating.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<T, CancellationToken, ValueTask>>(options);
    }

    public async ValueTask AddAsync(Func<T, CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
            throw new ArgumentNullException(nameof(workItem));

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<T, CancellationToken, ValueTask>> GetAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}