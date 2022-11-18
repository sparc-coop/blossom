using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
