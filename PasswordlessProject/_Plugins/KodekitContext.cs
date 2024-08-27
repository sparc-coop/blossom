using Microsoft.EntityFrameworkCore;
using Sparc.Blossom;

namespace PasswordlessProject;

public partial class KodekitContext(BlossomContextOptions options) : BlossomContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().ToContainer("Users").HasPartitionKey(x => x.UserId);
    }
}
