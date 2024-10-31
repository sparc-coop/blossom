using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Data;
using Sparc.Blossom.Server.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>
    (IRepository<T> users, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, PersistentComponentState state) 
    : BlossomAuthenticationStateProvider<T>(loggerFactory, scopeFactory, state), IBlossomAuthenticator 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.LoggedOut;

    public BlossomUser? User { get; set; }
    public IRepository<T> Users { get; } = users;
    public string? Message { get; set; }

    public override async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            User = await Users.FindAsync(principal.Id());
        }

        if (User == null)
        {
            User = BlossomUser.FromPrincipal(principal); 
            await Users.AddAsync((T)User);
        }

        return User!;
    }

    public virtual async IAsyncEnumerable<LoginStates> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public virtual IAsyncEnumerable<LoginStates> LogoutAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }
}
