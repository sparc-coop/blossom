using Microsoft.EntityFrameworkCore;
using Sparc.Kernel;
using Sparc.Realtime;

namespace Sparc.Data;

public class SparcContext : DbContext
{
    public SparcContext(DbContextOptions options, Publisher publisher) : base(options)
    {
        Publisher = publisher;
    }
    public Publisher Publisher { get; }
    PublishStrategy PublishStrategy = PublishStrategy.ParallelNoWait;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync();
        return result;
    }

    public void SetPublishStrategy(PublishStrategy strategy)
    {
        PublishStrategy = strategy;
    }

    async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker.Entries<Root>().Where(x => x.Entity._events != null && x.Entity._events.Any());
        var domainEvents = domainEntities.SelectMany(x => x.Entity._events!).ToList();
        domainEntities.ToList().ForEach(entity => entity.Entity._events!.Clear());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Publisher.Publish(domainEvent, PublishStrategy);
            });

        await Task.WhenAll(tasks);

        foreach (var entity in domainEntities)
            entity.Entity._events!.Clear();
    }
}
