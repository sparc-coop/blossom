using Microsoft.EntityFrameworkCore;

namespace TemplateWebNET7.Features._Plugins
{
    public class TemplateWebNET7Context : BlossomContext
    {
        public DbSet<User> Users => Set<User>();

        public TemplateWebNET7Context(DbContextOptions options, Publisher publisher, IHttpContextAccessor http) : base(options, publisher, http)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
#if (AddCosmos)
            builder.Entity<User>().ToContainer("Users").HasPartitionKey(x => x.UserId);
#endif
#if (AddSQL)
            builder.Entity<User>().ToTable("Users");
#endif
        }
    }
}
