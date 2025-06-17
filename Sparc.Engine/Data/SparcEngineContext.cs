using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;

internal class SparcEngineContext(DbContextOptions<SparcEngineContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);

        
        model.Entity<PouchDatum>().ToContainer("Datum")
            .HasPartitionKey(x => new { x.RealmId, x.Id })
            .HasKey(x => x.Id);

        model.Entity<ReplicationLog>().ToContainer("ReplicationLog")
            .HasPartitionKey(x => new { x.RealmId, x.Id })
            .HasKey(x => x.Id);
    }
}