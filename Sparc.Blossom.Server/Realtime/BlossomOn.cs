using MediatR;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public abstract class BlossomOn<T> : INotificationHandler<T> where T : BlossomEvent
{
    public abstract Task ExecuteAsync(T item);

    public async Task Handle(T request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request);
    }
}

public class AddBlossomEntity<T>(IRepository<T> repository) : BlossomOn<BlossomEntityAdded<T>> where T : BlossomEntity
{
    public override async Task ExecuteAsync(BlossomEntityAdded<T> e)
    {
        await repository.AddAsync(e.Entity);
    }
}