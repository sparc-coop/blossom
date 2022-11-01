using MediatR;
using Sparc.Blossom;

namespace Sparc.Realtime;

public abstract class RealtimeFeature<T> : INotificationHandler<T> where T : Notification
{
    public abstract Task ExecuteAsync(T item);

    public async Task Handle(T request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request);
    }
}