using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Kori;

public class ContentContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Page>().HasPartitionKey(x => new { x.Domain, x.Path }).ToContainer("content");
        builder.Entity<TextContent>().HasPartitionKey(x => new { x.Domain, x.Path }).ToContainer("content");
        builder.Entity<BlossomUser>().HasPartitionKey(x => x.UserId).ToContainer("users");
    }
}
