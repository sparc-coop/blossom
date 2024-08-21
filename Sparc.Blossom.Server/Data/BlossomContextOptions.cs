using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom;

public class BlossomContextOptions(DbContextOptions options, IPublisher publisher, IHttpContextAccessor auth)
{
    public DbContextOptions DbContextOptions { get; set; } = options;
    public IPublisher Publisher { get; set; } = publisher;
    public IHttpContextAccessor HttpContextAccessor { get; set; } = auth;
}
