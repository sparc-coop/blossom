using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;

internal class BlossomCloudContext(DbContextOptions<BlossomCloudContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);

        
        model.Entity<Datum>().ToContainer("Datum")
            .HasPartitionKey(x => new { x.TenantId, x.UserId, x.DatasetId })
            .HasKey(x => x.Id);
    }
}