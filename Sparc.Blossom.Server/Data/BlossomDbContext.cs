using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class BlossomDbContext<T>(DbContextOptions options, BlossomNotifier notifier) : DbContext(options) where T : Entity
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new BlossomPropertyDiscoveryConvention());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        var entities = ChangeTracker.Entries<T>().Select(x => x.Entity);
        await notifier.NotifyAsync(entities);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<T>();
    }
}
