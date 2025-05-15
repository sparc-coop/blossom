using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;

internal class BlossomCloudContext(DbContextOptions<BlossomCloudContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);

        
        model.Entity<Datum>().ToContainer("Datum")
            .HasPartitionKey(x => new { x.TenantId, x.UserId, x.DatasetId })
            .HasKey(x => x.Id);

        model.Entity<ReplicationLog>().ToContainer("ReplicationLog")
            .HasPartitionKey(x => new { x.TenantId, x.UserId, x.DatabaseId })
            .HasKey(x => x.Id);
    }
}