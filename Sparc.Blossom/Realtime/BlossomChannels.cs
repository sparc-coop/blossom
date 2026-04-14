using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Sparc.Blossom.Realtime;

public class BlossomChannels(IServiceScopeFactory scopes) : BackgroundService, IBlossomChannels
{
    readonly BlossomQueue<BlossomEvent> MainChannel = new();
    static readonly ConcurrentDictionary<string, BlossomQueue<BlossomEvent>> Channels = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BlossomEvent ev in MainChannel.Reader.ReadAllAsync(stoppingToken))
            await Consume(ev, stoppingToken);
    }

    public IAsyncEnumerable<BlossomEvent> Get(string subscriptionId)
    {
        if (!Channels.TryGetValue(subscriptionId, out var channel))
            return AsyncEnumerable.Empty<BlossomEvent>();

        return channel.Reader.ReadAllAsync();
    }

    public async Task Publish(BlossomEvent ev, CancellationToken cancellationToken = default)
    {
        if (!Channels.TryGetValue(ev.SubscriptionId, out var queue))
        {
            queue = new BlossomQueue<BlossomEvent>();
            Channels[ev.SubscriptionId] = queue;
        }

        await queue.Writer.WriteAsync(ev, cancellationToken);
    }

    public async Task Consume(BlossomEvent ev, CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var handlerType = typeof(BlossomOn<>).MakeGenericType(ev.GetType());
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        await Parallel.ForEachAsync(handlers, cancellationToken, async (handler, ct) =>
         {
             if (ct.IsCancellationRequested)
                 return;

             if (handler is IBlossomOn blossomOn)
                 await blossomOn.ExecuteAsync(ev);
         });
    }
}