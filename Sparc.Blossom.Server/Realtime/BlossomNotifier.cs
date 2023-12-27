using MediatR;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class BlossomNotifier
{
    private readonly ServiceFactory _serviceFactory;

    public PublishStrategy PublishStrategy { get; private set; } = PublishStrategy.ParallelNoWait;

    public BlossomNotifier(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;

        PublishStrategies[PublishStrategy.Async] = new BlossomMediator(_serviceFactory, AsyncContinueOnException);
        PublishStrategies[PublishStrategy.ParallelNoWait] = new BlossomMediator(_serviceFactory, ParallelNoWait);
        PublishStrategies[PublishStrategy.ParallelWhenAll] = new BlossomMediator(_serviceFactory, ParallelWhenAll);
        PublishStrategies[PublishStrategy.ParallelWhenAny] = new BlossomMediator(_serviceFactory, ParallelWhenAny);
        PublishStrategies[PublishStrategy.SyncContinueOnException] = new BlossomMediator(_serviceFactory, SyncContinueOnException);
        PublishStrategies[PublishStrategy.SyncStopOnException] = new BlossomMediator(_serviceFactory, SyncStopOnException);
    }

    public IDictionary<PublishStrategy, IMediator> PublishStrategies = new Dictionary<PublishStrategy, IMediator>();

    public void SetPublishStrategy(PublishStrategy strategy)
    {
        PublishStrategy = strategy;
    }

    public async Task NotifyAsync(IEnumerable<Entity> entities)
    {
        var domainEvents = entities.SelectMany(x => x.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }

    public Task Publish<TNotification>(TNotification notification)
    {
        return Publish(notification, PublishStrategy, default);
    }

    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy)
    {
        return Publish(notification, strategy, default);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken)
    {
        return Publish(notification, PublishStrategy, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken cancellationToken)
    {
        if (!PublishStrategies.TryGetValue(strategy, out var mediator))
        {
            throw new ArgumentException($"Unknown strategy: {strategy}");
        }

        if (notification is null)
            throw new ArgumentException("Notification is null");

        return mediator.Publish(notification, cancellationToken);
    }

    private Task ParallelWhenAll(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            tasks.Add(Task.Run(() => handler(notification, cancellationToken)));
        }

        return Task.WhenAll(tasks);
    }

    private Task ParallelWhenAny(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            tasks.Add(Task.Run(() => handler(notification, cancellationToken)));
        }

        return Task.WhenAny(tasks);
    }

    private Task ParallelNoWait(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            Task.Run(() => handler(notification, cancellationToken));
        }

        return Task.CompletedTask;
    }

    private async Task AsyncContinueOnException(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                tasks.Add(handler(notification, cancellationToken));
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
            {
                exceptions.Add(ex);
            }
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            exceptions.AddRange(ex.Flatten().InnerExceptions);
        }
        catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
        {
            exceptions.Add(ex);
        }

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }

    private async Task SyncStopOnException(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await handler(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SyncContinueOnException(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> handlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                exceptions.AddRange(ex.Flatten().InnerExceptions);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }
}
