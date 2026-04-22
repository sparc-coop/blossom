using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Sparc.Blossom.Realtime;

public class BlossomJobProcessor(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BlossomJob job in BlossomChannels.Jobs.Reader.ReadAllAsync(stoppingToken))
            await Run(job, stoppingToken);
    }

    public async Task Run(BlossomJob job, CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var parameters = job.Action.Method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i <  parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var service = scope.ServiceProvider.GetService(paramType) 
                ?? throw new InvalidOperationException($"No service registered for type {paramType.FullName}");
            args[i] = service;
        }

        var result = job.Action.DynamicInvoke(args);
        if (result is Task taskResult)
            await taskResult;

        BlossomChannels.Complete(job);
    }
}

//public class BlossomChannelProcessor(IServiceScopeFactory scopes) : BackgroundService
//{
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        await foreach (BlossomEvent ev in BlossomChannels.Jobs.Reader.ReadAllAsync(stoppingToken))
//            await Run(job, stoppingToken);
//    }

//    public async Task Run(BlossomJob job, CancellationToken cancellationToken = default)
//    {
//        using var scope = scopes.CreateScope();
//        var handlerType = typeof(BlossomOn<>).MakeGenericType(ev.GetType());
//        var handlers = scope.ServiceProvider.GetServices(handlerType);

//        await Parallel.ForEachAsync(handlers, cancellationToken, async (handler, ct) =>
//        {
//            if (ct.IsCancellationRequested)
//                return;

//            if (handler is IBlossomOn blossomOn)
//                await blossomOn.ExecuteAsync(ev);
//        });
//    }
//}

public record BlossomJob(string Id, Delegate Action, BlossomChannel<BlossomEvent> Channel);
public class BlossomChannels() : IBlossomChannels
{
    public static string DefaultSource { get; set; } = "https://engine.sparc.coop";
    internal static readonly BlossomChannel<BlossomJob> Jobs = new();
    static readonly ConcurrentDictionary<string, BlossomChannel<BlossomEvent>> Channels = [];

    public IAsyncEnumerable<BlossomEvent> Watch(string source)
    {
        if (!Channels.TryGetValue(source, out var channel))
            return AsyncEnumerable.Empty<BlossomEvent>();

        return channel.Reader.ReadAllAsync();
    }

    public static BlossomChannel<BlossomEvent> GetOrCreate(string source)
    {
        if (!Channels.TryGetValue(source, out var queue))
        {
            queue = new BlossomChannel<BlossomEvent>();
            Channels[source] = queue;
        }

        return queue;
    }

    public async Task<string> Publish<T>(T ev, CancellationToken cancellationToken = default)
        => await Publish(DefaultSource, ev, cancellationToken);

    public async Task<string> Publish<T>(string source, T ev, CancellationToken cancellationToken = default)
    {
        var blossomEvent = new BlossomEvent<T>(source, ev);
        
        var queue = GetOrCreate(source);
        await queue.Writer.WriteAsync(blossomEvent, cancellationToken);
        return blossomEvent.Id;
    }

    public async Task<string> Execute(Delegate action)
    {
        var id = Guid.NewGuid().ToString();
        await Execute(id, action);
        return id;
    }

    public static async Task Execute(string id, Delegate action)
    {
        var queue = GetOrCreate(id);
        var job = new BlossomJob(id, action, queue);
        await Jobs.Writer.WriteAsync(job);
    }

    internal static void Complete(BlossomJob job)
    {
        job.Channel.Writer.TryComplete();
        Channels.TryRemove(job.Id, out _);
    }
}