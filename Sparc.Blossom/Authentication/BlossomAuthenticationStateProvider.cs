using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticationStateProvider<T>(IBlossomCloud cloud) : AuthenticationStateProvider where T : BlossomUser
{
    BlossomUser? User;
    
    public IBlossomCloud Cloud { get; } = cloud;

    public virtual Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(BlossomUser.FromPrincipal(principal));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        User ??= await Cloud.UserInfo();
        var principal = User.Login();
        return new AuthenticationState(principal);
    }
}
