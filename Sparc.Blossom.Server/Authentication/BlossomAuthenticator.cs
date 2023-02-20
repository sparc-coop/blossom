using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public abstract class BlossomAuthenticator
{
    public BlossomAuthenticator(IConfiguration config)
    {
        Config = config;
    }

    public IConfiguration Config { get; }

    public abstract Task<BlossomUser?> LoginAsync(string userName, string password);
    public abstract Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal);
}
