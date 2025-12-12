using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data.Pouch;
using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Engine;

internal class SparcEngineContext(DbContextOptions<SparcEngineContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PouchDatum>().ToContainer("Data")
            .HasPartitionKey(x => new { x.Db, x.PouchId })
            .HasKey(x => x.Id);

        model.Entity<ReplicationLog>().ToContainer("ReplicationLogs")
            .HasPartitionKey(x => new { x.Db, x.PouchId })
            .HasKey(x => x.Id);

        model.Entity<TextContent>().ToContainer("TextContent")
            .HasPartitionKey(x => new { x.Domain, x.LanguageId })
            .HasKey(x => x.Id);

        model.Entity<SparcDomain>().ToContainer("Domains")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);

        model.Entity<Page>().ToContainer("Pages")
            .HasPartitionKey(x => x.Domain)
            .HasKey(x => x.Id);

        model.Entity<SparcOrder>().ToContainer("Orders")
            .HasPartitionKey(x => x.UserId)
            .HasKey(x => x.Id);

        model.Entity<UserCharge>().ToContainer("UserCharges")
            .HasPartitionKey(x => x.UserId)
            .HasKey(x => x.Id);

        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);

        model.Entity<BlossomEvent>()
          .ToContainer("Events")
          .HasPartitionKey(e => e.SpaceId)
          .HasKey(x => x.Id);

        model.Entity<BlossomSpace>()
            .ToContainer("Spaces")
            .HasPartitionKey(s => s.Domain)
            .HasKey(x => x.Id);

        model.Entity<BlossomVector>()
            .ToContainer("Vectors")
            .HasPartitionKey(v => v.SpaceId)
            .HasKey(x => x.Id);
    }
}