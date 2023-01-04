using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

internal class PasswordlessAuthenticator : BlossomAuthenticator
{
    public PasswordlessAuthenticator(IConfiguration config) : base(config)
    {
    }

    public override Task<BlossomUser?> LoginAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public override Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }
}
