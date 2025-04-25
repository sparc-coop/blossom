using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;

internal class BlossomCloudContext(DbContextOptions<BlossomCloudContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);
    }
}