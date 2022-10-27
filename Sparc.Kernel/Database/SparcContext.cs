using Microsoft.EntityFrameworkCore;
using Sparc.Core;
using Sparc.Realtime;

namespace Sparc.Kernel.Database;

public class SparcContext : DbContext
{
    public SparcContext(DbContextOptions options, Publisher publisher) : base(options)
    {
        Publisher = publisher;
    }
    public Publisher Publisher { get; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync();
        return result;
    }

    async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker.Entries<ISparcRoot>().Where(x => x.Entity.Events != null && x.Entity.Events.Any());
        var domainEvents = domainEntities.SelectMany(x => x.Entity.Events!).ToList();
        domainEntities.ToList().ForEach(entity => entity.Entity.Events!.Clear());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Publisher.Publish(domainEvent, PublishStrategy.ParallelNoWait);
            });

        await Task.WhenAll(tasks);

        foreach (var entity in domainEntities)
            entity.Entity.Events!.Clear();
    }
}
