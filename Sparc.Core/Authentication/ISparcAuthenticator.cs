using System.Security.Claims;

namespace Sparc.Core;

public interface ISparcAuthenticator
{
    public Task<bool> LoginAsync();
    public Task<ClaimsPrincipal> LoginAsync(string returnUrl);
    public Task LogoutAsync();
    public ClaimsPrincipal User { get; set; }
}