using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom.Platforms.Browser;

public class BlossomBrowserAuthenticationStateProvider<T>(IRepository<BlossomUser> users)
    : AuthenticationStateProvider() where T : BlossomUser
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = users.Query.FirstOrDefault();
        var principal = user?.ToPrincipal() ?? new ClaimsPrincipal(new ClaimsIdentity());

        return Task.FromResult(new AuthenticationState(principal));
    }
}
