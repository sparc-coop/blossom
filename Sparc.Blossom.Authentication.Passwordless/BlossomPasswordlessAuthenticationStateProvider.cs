using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication.Passwordless;

public class BlossomPasswordlessAuthenticationStateProvider<T> : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
