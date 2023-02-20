using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Data;

public class BlossomContext : DbContext
{
    public BlossomContext(DbContextOptions options, Publisher publisher, IHttpContextAccessor http) : base(options)
    {
        Publisher = publisher;
        Http = http;
    }
    
    public Publisher Publisher { get; }
    protected IHttpContextAccessor Http { get; }
    protected ClaimsPrincipal? User => Http.HttpContext?.User;

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
        var domainEvents = ChangeTracker.Entries<Root>().SelectMany(x => x.Entity.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Publisher.Publish(domainEvent, PublishStrategy);
            });

        await Task.WhenAll(tasks);
    }
}
