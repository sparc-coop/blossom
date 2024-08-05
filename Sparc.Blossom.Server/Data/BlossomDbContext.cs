using MediatR;
using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public class BlossomDbContext(BlossomDbContextOptions options) : DbContext(options.DbContextOptions)
{
    public IPublisher Publisher { get; } = options.Publisher;
    public IHttpContextAccessor Http { get; } = options.HttpContextAccessor;
    public string UserId => Http?.HttpContext?.User?.Identity?.IsAuthenticated == true ? Http.HttpContext.User.Id() : "anonymous";

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
                await Publisher.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}
