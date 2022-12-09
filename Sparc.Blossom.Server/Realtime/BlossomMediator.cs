using MediatR;

namespace Sparc.Blossom.Realtime;

public class BlossomMediator : Mediator
{
    private Func<IEnumerable<Func<MediatR.INotification, CancellationToken, Task>>, MediatR.INotification, CancellationToken, Task> _publish;

    public BlossomMediator(ServiceFactory serviceFactory, Func<IEnumerable<Func<MediatR.INotification, CancellationToken, Task>>, MediatR.INotification, CancellationToken, Task> publish) : base(serviceFactory)
    {
        _publish = publish;
    }

    protected override Task PublishCore(IEnumerable<Func<MediatR.INotification, CancellationToken, Task>> allHandlers, MediatR.INotification notification, CancellationToken cancellationToken)
    {
        return _publish(allHandlers, notification, cancellationToken);
    }
}