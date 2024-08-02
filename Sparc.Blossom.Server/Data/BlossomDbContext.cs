using MediatR;
using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public class BlossomDbContext(DbContextOptions options, IPublisher publisher, IHttpContextAccessor auth) : DbContext(options)
{
    public IPublisher Publisher { get; } = publisher;
    public IHttpContextAccessor Auth { get; } = auth;
    public string UserId => Auth?.HttpContext?.User?.Identity?.IsAuthenticated == true ? Auth.HttpContext.User.Id() : "anonymous";

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

    public void SetPublishStrategy(PublishStrategy strategy)
    {
        //Notifier.SetPublishStrategy(strategy);
    }

    async Task NotifyAsync()
    {
        var domainEvents = ChangeTracker.Entries<BlossomEntity>().SelectMany(x => x.Entity.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Publisher.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}
