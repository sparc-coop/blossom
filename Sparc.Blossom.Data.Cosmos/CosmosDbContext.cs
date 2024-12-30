using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Sparc.Blossom.Data.Cosmos
{
    public class CosmosDbContext(DbContextOptions dbOptions, IPublisher publisher) : DbContext(dbOptions)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new BlossomPropertyDiscoveryConvention());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            await NotifyAsync();

            return result;
        }

        async Task NotifyAsync()
        {
            var domainEvents = ChangeTracker.Entries<BlossomEntity>().SelectMany(x => x.Entity.Publish());


            var tasks = domainEvents
                .Select(async (domainEvent) =>
                {
                    await publisher.Publish(domainEvent);
                });

            await Task.WhenAll(tasks);
        }

    }
}
