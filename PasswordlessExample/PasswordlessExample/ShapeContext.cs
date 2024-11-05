using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PasswordlessExample;

internal class ShapeContext : DbContext
{
    public ShapeContext(DbContextOptions<ShapeContext> options, IHttpContextAccessor accessor) : base(options)
    {
        Accessor = accessor;
    }

    public string? UserExternalId =>
        Accessor.HttpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;

    private IHttpContextAccessor Accessor { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("UserTest");
            
            entity.Ignore(e => e.Claims);
            entity.Ignore(e => e.MultiClaims);
            entity.Ignore(e => e.SecurityStamp);
            //entity.Ignore(e => e.LoginProviderKey);
            //entity.Ignore(e => e.UserName);

            entity.Property(e => e.UserName).HasColumnName("Name");
            entity.Property(e => e.LoginProviderKey).HasColumnName("ExternalId");
        });
    }
}