using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public interface IBlossomApplicationBuilder
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    void AddAuthentication<TUser>() where TUser : BlossomUser, new();
    public IBlossomApplication Build();
}
