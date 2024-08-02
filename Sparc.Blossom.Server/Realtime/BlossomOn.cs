using MediatR;

namespace Sparc.Blossom.Realtime;

public abstract class BlossomOn<T> : INotificationHandler<T> where T : BlossomEvent
{
    public abstract Task ExecuteAsync(T item);

    public async Task Handle(T request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request);
    }
}