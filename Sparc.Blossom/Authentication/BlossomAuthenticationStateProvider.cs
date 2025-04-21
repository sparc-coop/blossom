using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticationStateProvider<T>(IRepository<T> users) : AuthenticationStateProvider where T : BlossomUser
{
    public IRepository<T> Users { get; } = users;

    public virtual async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        return await Users.FindAsync(principal.Id()) ?? BlossomUser.FromPrincipal(principal);

    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
