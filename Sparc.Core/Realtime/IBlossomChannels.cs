namespace Sparc.Blossom.Realtime;

public interface IBlossomChannels
{
    IAsyncEnumerable<BlossomEvent> Get(string subscriptionId);
    Task Consume(BlossomEvent ev, CancellationToken cancellationToken = default);
    Task Publish(BlossomEvent ev, CancellationToken cancellationToken = default);
}
