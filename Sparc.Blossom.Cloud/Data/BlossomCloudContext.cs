using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

internal class BlossomCloudContext(DbContextOptions<BlossomCloudContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<BlossomUser>().ToContainer("Users")
            .HasPartitionKey(x => x.UserId);
        model.Entity<TextContent>().ToContainer("TextContent")
            .HasPartitionKey(x => x.Id);
    }
}