using System.Collections.Concurrent;
using System.Net.ServerSentEvents;

namespace Sparc.Blossom.Realtime;

public class BlossomEvents() : IBlossomEvents
{
    internal readonly BlossomChannel<BlossomJob> Jobs = new();
    internal readonly BlossomChannel<BlossomEvent> Events = new();
    readonly ConcurrentDictionary<string, BlossomChannel<BlossomEvent>> Channels = [];

    public async IAsyncEnumerable<BlossomEvent> Watch(string source)
    {
        if (!Channels.TryGetValue(source, out var channel))
            yield break;

        await foreach (var ev in channel.Reader.ReadAllAsync())
            yield return ev;

        yield return new BlossomEvent(source) { Type = "done" };
    }

    public async IAsyncEnumerable<SseItem<BlossomEvent>> GetSseStream(string source)
    {
        if (Channels.TryGetValue(source, out var channel))
            await foreach (var ev in channel.Reader.ReadAllAsync())
                yield return new(ev, ev.Type);

        yield return new(new(source) { Type = "done" }, "done");
    }

    public BlossomChannel<BlossomEvent> GetOrCreate(string source)
    {
        if (!Channels.TryGetValue(source, out var queue))
        {
            queue = new BlossomChannel<BlossomEvent>();
            Channels[source] = queue;
        }

        return queue;
    }

    public async Task Publish<T>(T ev, CancellationToken cancellationToken = default) where T : BlossomEvent
        => await Events.Writer.WriteAsync(ev, cancellationToken);

    public async Task Publish<T>(string source, T ev, CancellationToken cancellationToken = default)
    {
        var blossomEvent = new BlossomEvent<T>(source, ev);

        var channel = GetOrCreate(source);
        await channel.Writer.WriteAsync(blossomEvent, cancellationToken);
    }

    public async Task<string> Execute(Delegate action)
    {
        var id = Guid.NewGuid().ToString();
        await Execute(id, action);
        return id;
    }

    public async Task Execute(string id, Delegate action)
    {
        var queue = GetOrCreate(id);
        var job = new BlossomJob(id, action, queue);
        await Jobs.Writer.WriteAsync(job);
    }

    internal static void Complete(BlossomJob job)
    {
        job.Channel.Writer.TryComplete();
        //Channels.TryRemove(job.Id, out _);
    }
}