using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public class BlossomContext(BlossomContextOptions options) : DbContext(options.DbContextOptions)
{
    protected BlossomContextOptions Options { get; } = options;
    public string UserId => Options.HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? Options.HttpContextAccessor.HttpContext.User.Id() : "anonymous";

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
                await Options.Publisher.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}