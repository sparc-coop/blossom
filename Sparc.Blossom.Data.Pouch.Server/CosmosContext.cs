using Microsoft.EntityFrameworkCore;


namespace Sparc.Blossom.Data.Pouch
{
    internal class CosmosContext : DbContext
    {
        public CosmosContext(DbContextOptions<CosmosContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure your entity mappings here
            base.OnModelCreating(modelBuilder);
        }
    }
}
